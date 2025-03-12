namespace Ais.Office.Controllers
{
    using Ais.Data.Base.Ais;
    using Ais.Data.Models.Attachment;
    using Ais.Data.Models.Document;
    using Ais.Data.Models.Helpers;
    using Ais.Data.Models.Reporting;
    using Ais.Data.Models.Signature;
    using Ais.Infrastructure.BaseTypes;
    using Ais.Infrastructure.KendoExt;
    using Ais.Office.Controllers.Documents;
    using Ais.Office.Hubs;
    using Ais.Office.Services.DocumentStatusService;
    using Ais.Office.ViewModels.Sign;
    using Ais.Services.Ais;
    using Ais.Utilities.Exception;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.Storage;
    using Ais.WebUtilities.Enums;
    using Ais.WebUtilities.Extensions;
    using IO.SignTools.Contracts;
    using IO.SignTools.Models;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Class SignController.
    /// Implements the <see cref="BaseController" />
    /// </summary>
    /// <seealso cref="BaseController" />
    [Authorize]
    public class SignController : BaseController
    {
        private readonly IStorageService storageService;
        private readonly IIOSignToolsService signToolsService;
        private readonly IHubContext<SignalrHub> signatureHub;
        private readonly IDocumentStatusService documentStatusService;
        private readonly SignatureOptions defaultSignatureOptions;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IDocumentService documentService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SignController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="storageService">The storage service.</param>
        /// <param name="signToolsService">The sign tools service.</param>
        /// <param name="signatureHub">The signature hub.</param>
        /// <param name="documentStatusService">The document status service.</param>
        /// <param name="options">The signature options.</param>
        /// <param name="contextManager">The context manager.</param>
        /// <param name="documentService">The document service.</param>
        public SignController(
            ILogger<BaseController> logger,
            IStringLocalizer localizer,
            IStorageService storageService,
            IIOSignToolsService signToolsService,
            IHubContext<SignalrHub> signatureHub,
            IDocumentStatusService documentStatusService,
            IOptions<SignatureOptions> options,
            IDataBaseContextManager<AisDbType> contextManager,
            IDocumentService documentService)
            : base(logger, localizer)
        {
            this.storageService = storageService;
            this.signToolsService = signToolsService;
            this.signatureHub = signatureHub;
            this.documentStatusService = documentStatusService;
            this.contextManager = contextManager;
            this.documentService = documentService;
            this.defaultSignatureOptions = options.Value;
        }

        /// <summary>
        /// Signs the PDF.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>IActionResult.</returns>
        [HttpGet]
        public IActionResult SignPdf(SignViewModel model = null)
        {
            this.ModelState.Clear();
            this.InitViewTitleAndBreadcrumbs(this.Localizer["Sign"]);
            return this.ReturnView("_Sign", model?.Url.IsNotNullOrEmpty() == true ? model : null);
        }

        /// <summary>
        /// Signs the PDF post.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> SignPdfPost(SignViewModel model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.SignPdf(model);
            }

            var signatureOptions = await this.GetSignatureOptionsAsync(model.EntryType, model.Id);
            var name = "sign.pdf";
            var index = model.Signature.IndexOf(',') + 1;
            var signStream = new MemoryStream(Convert.FromBase64String(model.Signature.Substring(index)));
            var fileStream = await this.storageService.DownloadAsync(new[] { model.Url });
            await using var signPdfStream = await PdfFormatProviderExt.AttachImageToLastPdfPageAsync(
                fileStream,
                signStream,
                signatureOptions.Client.OffsetX ?? 0,
                signatureOptions.Client.OffsetY ?? 0,
                signatureOptions.Client.Width ?? 0,
                signatureOptions.Client.Height ?? 0);
            var signPdf = await this.storageService.UploadAsync(
                new FormFile(
                    signPdfStream,
                    0,
                    signPdfStream.Length,
                    name,
                    name));

            var digitSignModel = new SignPdfViewModel
            {
                Id = model.Id,
                EntryType = model.EntryType,
                Status = model.Status,
                AttachmentType = model.AttachmentType,
                Operation = model.Operation,
                OriginPath = model.Url,
                Url = signPdf.Url,
            };
            digitSignModel.CalculateCheck();
            var digitSignUrl = this.Url.DynamicAction(
                nameof(this.DigitSignPdf),
                this.GetType(),
                digitSignModel);

            await this.signatureHub.SendReloadToDigitSignPdfAsync(this.User, digitSignUrl);

            this.ShowMessage(MessageType.Success, this.Localizer["SuccessfulSigning"]);

            var url = this.Url.DynamicAction(
                nameof(this.SignPdf),
                this.GetType());
            return this.Redirect(url);
        }

        /// <summary>
        /// Digits the sign PDF.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>IActionResult.</returns>
        /// <exception cref="System.ArgumentNullException">FilePath</exception>
        /// <exception cref="System.ArgumentException">Invalid file path.</exception>
        [HttpGet]
        public async Task<IActionResult> DigitSignPdf(SignPdfViewModel model)
        {
            this.ModelState.Clear();
            if (model != null)
            {
                if (model.Url.IsNotNullOrEmpty() != true && !model.FileId.HasValue)
                {
                    throw new ArgumentException($"One of fields '{nameof(model.Url)}' or '{nameof(model.FileId)}' is required!");
                }

                var attachment = new Attachment { Url = model.Url, Id = model.FileId };
                await this.storageService.InitMetadataAsync(new[] { attachment });
                if (attachment.Size <= 0)
                {
                    throw new ArgumentException("Invalid file path.");
                }

                if (Path.GetExtension(attachment.Name)?.Equals($".{Enum.GetName(ExportType.Pdf)!}", StringComparison.InvariantCultureIgnoreCase) != true)
                {
                    throw new ArgumentException("Invalid file extension.");
                }
            }

            if (!this.HttpContext.Request.IsAjaxRequest())
            {
                this.InitViewTitleAndBreadcrumbs(this.Localizer["Sign"]);
            }

            return this.ReturnView("_DigitSignPdf", model);
        }

        /// <summary>
        /// Prepares the digit sign PDF.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>IActionResult.</returns>
        /// <exception cref="System.ArgumentException">Invalid model.</exception>
        [HttpPost]
        public async Task<IActionResult> PrepareDigitSignPdf(SignPdfSignatureViewData data)
        {
            if (!this.ModelState.IsValid)
            {
                throw new ArgumentException("Invalid model.");
            }

            var signatureOptions = await this.GetSignatureOptionsAsync(data.EntryType, data.Id);
            VisualSignaturOptions visualOptions = null;
            if (data.Visual)
            {
                visualOptions = new VisualSignaturOptions
                {
                    Cordinates = new iText.Kernel.Geom.Rectangle(
                        signatureOptions.Employee.OffsetX ?? 0,
                        signatureOptions.Employee.OffsetY ?? 0,
                        signatureOptions.Employee.Width ?? 0,
                        signatureOptions.Employee.Height ?? 0),
                    PageNumber = data.Page, // Last page
                    Reason = data.Reason,
                    Signer = data.SignerName,
                    SignerCertificate = data.SignerCert
                };
            }

            string hash;
            string tempPdfId;
            await using var pdfStream = await this.storageService.DownloadAsync(
                data.FilePath.IsNotNullOrEmpty() ? new[] { data.FilePath } : null,
                data.FileId.HasValue ? new[] { data.FileId.Value } : null);
            if (visualOptions != null)
            {
                (hash, tempPdfId) = await this.signToolsService.GetPdfHashForVisualSignature(pdfStream, visualOptions);
            }
            else
            {
                (hash, tempPdfId) = await this.signToolsService.GetPdfHash(pdfStream, data.Reason);
            }

            return this.Json(new { hash, tempPdfId });
        }

        /// <summary>
        /// Digits the sign PDF post.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="refresh">The flag is action refresh.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost]
        public async Task<IActionResult> DigitSignPdfPost(SignPdfViewModel model, bool refresh = false)
        {
            if (refresh)
            {
                var signModel = new SignViewModel
                {
                    Id = model.Id,
                    EntryType = model.EntryType,
                    Status = model.Status,
                    AttachmentType = model.AttachmentType,
                    Url = model.OriginPath,
                    FileId = model.FileId,
                };
                signModel.CalculateCheck();
                var signUrl = this.Url.DynamicAction(
                    nameof(this.SignPdf),
                    this.GetType(),
                    signModel);

                await this.signatureHub.SendReloadToTabletSignAsync(this.User, signUrl);
                return await this.DigitSignPdf(null);
            }

            if (!this.ModelState.IsValid)
            {
                return await this.DigitSignPdf(model);
            }

            try
            {
                // TODO validate login user cert
                ////var (cert, time) = CryptoManager.GetSignatureInfoFromCms(Convert.FromBase64String(model.Signature));

                var (signedPdf, _) = await this.signToolsService.EmbedPdfSignature(model.TempPdfId, model.Signature);

                using var ms = new MemoryStream(signedPdf);
                signedPdf = this.signToolsService.AddLTV(ms);

                var attachment = new Attachment { Id = model.FileId, Url = model.OriginPath ?? model.Url };
                await this.storageService.InitMetadataAsync(new[] { attachment });

                var name = attachment.Name ?? "sign.pdf";
                var signPdfStream = new MemoryStream(signedPdf);
                var signPdf = await this.storageService.UploadAsync(
                    new FormFile(
                        signPdfStream,
                        0,
                        signPdfStream.Length,
                        name,
                        name));

                signPdf!.Type = new AttachmentType
                {
                    Id = EnumHelper.GetAttachmentTypeIdByAttachmentType(model.AttachmentType),
                };
                signPdf.RelDocType = RelDocType.Attachment;

                string url = null;
                switch (model.EntryType)
                {
                    case EntryType.OutDocument:
                        {
                            url = this.Url.DynamicAction(
                                        nameof(OutDocumentsController.Info),
                                        typeof(OutDocumentsController),
                                        new { id = model.Id });
                            await this.documentStatusService.SetDocumentStatusAsync(
                                model.Id,
                                Guid.Empty,
                                model.EntryType,
                                model.Status,
                                model.Operation.IsNotNullOrEmpty() ? model.Operation : nameof(this.DigitSignPdfPost),
                                new List<Attachment> { signPdf });
                            break;
                        }
                }

                this.ShowMessage(MessageType.Success, this.Localizer["SuccessfulSigning"]);
                if (url.IsNullOrEmpty() && this.HttpContext.Request.IsAjaxRequest())
                {
                    return this.Json(
                        new
                        {
                            success = true,
                            file = signPdf,
                            model = model
                        });
                }

                return this.RedirectToUrl(url ?? this.GetDefaultUrl());
            }
            catch (SignatureValidationException e)
            {
                throw new UserException(e.Message);
            }
        }

        private async Task<SignatureOptions> GetSignatureOptionsAsync(EntryType type, Guid id)
        {
            SignatureOptions dbOptions;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbOptions = await this.documentService.GetSignatureOptionsAsync(type, id);
            }

            dbOptions ??= this.defaultSignatureOptions;
            dbOptions.Client ??= this.defaultSignatureOptions?.Client;
            if (dbOptions.Client != null)
            {
                dbOptions.Client.OffsetX ??= this.defaultSignatureOptions?.Client?.OffsetX;
                dbOptions.Client.OffsetY ??= this.defaultSignatureOptions?.Client?.OffsetY;
                dbOptions.Client.Width ??= this.defaultSignatureOptions?.Client?.Width;
                dbOptions.Client.Height ??= this.defaultSignatureOptions?.Client?.Height;
            }

            dbOptions.Employee ??= this.defaultSignatureOptions?.Employee;
            if (dbOptions.Employee != null)
            {
                dbOptions.Employee.OffsetX ??= this.defaultSignatureOptions?.Employee?.OffsetX;
                dbOptions.Employee.OffsetY ??= this.defaultSignatureOptions?.Employee?.OffsetY;
                dbOptions.Employee.Width ??= this.defaultSignatureOptions?.Employee?.Width;
                dbOptions.Employee.Height ??= this.defaultSignatureOptions?.Employee?.Height;
            }

            return dbOptions;
        }
    }
}
