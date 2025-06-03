namespace Integration.Api.Controllers
{
    using Ais.Data.Base.Ais;
    using Ais.Data.Common.Base;
    using Ais.Data.Models.OutAdmAct;
    using Ais.Data.Models.QueryModels.AdmAct;
    using Ais.Services.Ais;
    using Ais.Utilities.Extensions;

    using Integration.MRRBAdapter;

    using Microsoft.AspNetCore.Mvc;

    public class AdmActReportsController : BaseController
    {
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly ILogger<AdmActReportsController> logger;
        private readonly IOutAdmActService outAdmActService;

        public AdmActReportsController(
            IDataBaseContextManager<AisDbType> contextManager,
            ILogger<AdmActReportsController> logger,
            IOutAdmActService admActService)
        {
            this.contextManager = contextManager;
            this.logger = logger;
            this.outAdmActService = admActService;
        }

        [HttpGet]
        [Route("GetReportByCadNumber")]
        public async Task<IActionResult> GetReportByCadNumber(string cadNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(cadNumber))
                {
                    return this.NotFound();
                }

                var query = new AdmActRegiXQueryModel
                            {
                                CadIdentificator = cadNumber
                            };

                List<OutAdmAct> dbResult;
                await using (await this.contextManager.NewConnectionAsync())
                {
                    dbResult = await this.outAdmActService.SearchAdmActAsync(query);
                }

                var result = this.FillObject(dbResult);
                return this.Ok(result);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex.Message);
            }

            return this.NotFound();
        }

        [HttpGet]
        [Route("GetReportByUIN")]
        public async Task<IActionResult> GetReportByUIN(string uin)
        {
            var query = new AdmActRegiXQueryModel
                        {
                            EgnBulstat = uin
                        };

            List<OutAdmAct> dbResult;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbResult = await this.outAdmActService.SearchAdmActAsync(query);
            }

            var result = this.FillObject(dbResult);
            return this.Ok(result);
        }

        [HttpGet]
        [Route("GetReportByUIC")]
        public async Task<IActionResult> GetReportByUIC(string uic)
        {
            var query = new AdmActRegiXQueryModel
                        {
                            EgnBulstat = uic
                        };

            List<OutAdmAct> dbResult;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbResult = await this.outAdmActService.SearchAdmActAsync(query);
            }

            var result = this.FillObject(dbResult);
            return this.Ok(result);
        }

        [HttpGet]
        [Route("GetReportByBulstat")]
        public async Task<IActionResult> GetReportByBulstat(string bulstat)
        {
            var query = new AdmActRegiXQueryModel
                        {
                            EgnBulstat = bulstat
                        };

            List<OutAdmAct> dbResult;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbResult = await this.outAdmActService.SearchAdmActAsync(query);
            }

            var result = this.FillObject(dbResult);
            return this.Ok(result);
        }

        [HttpGet]
        [Route("GetReportByAdmAct")]
        public async Task<IActionResult> GetReportByAdmAct(
            string regNumber,
            DateTime regDate,
            string administration,
            string administrativeBody)
        {
            Guid? issuer = null;
            if (Guid.TryParse(administrativeBody, out var issureId))
            {
                issuer = issureId;
            }

            var model = new AdmActRegiXQueryModel
                        {
                            RegNum = regNumber,
                            RegDate = regDate,
                            Administration = !string.IsNullOrEmpty(administration) ? $"%{administration}%" : null,
                            IssuerId = issuer
                        };

            List<OutAdmAct> dbResult;
            await using (await this.contextManager.NewConnectionAsync())
            {
                dbResult = await this.outAdmActService.SearchAdmActAsync(model);
            }

            var result = this.FillObject(dbResult);
            return this.Ok(result);
        }

        private AdmActInfoResponseType FillObject(List<OutAdmAct> documents)
        {
            var response = new AdmActInfoResponseType
                           {
                               AmdActResponse = new List<AmdActResponseType>()
                           };

            foreach (var doc in documents)
            {
                var applicants = new List<Applicant>();

                foreach (var applicant in doc.Applicants)
                {
                    var applicantData = new Applicant
                                        {
                                            FullName = applicant?.Recipient?.FullName,
                                            Address =
                                                applicant?.Recipient?.ContactData?.FullDescription ?? string.Empty,
                                            EgnBulstat = applicant?.Recipient?.EgnBulstat ?? string.Empty,
                                            Type = applicant?.Recipient?.Type?.Name ?? string.Empty,
                                            Phone = applicant?.Recipient?.Phone ?? string.Empty,
                                            QualityName = applicant?.AuthorQuality?.Type?.Name ?? string.Empty
                                        };

                    applicants.Add(applicantData);
                }

                var actObject = new List<ActObjectType>();

                if (doc.Object.AdmActObjects.IsNotNullOrEmpty())
                {
                    foreach (var actObj in doc.Object.AdmActObjects)
                    {
                        var obj = new ActObjectType
                                  {
                                      Province = actObj?.Province?.Name != null
                                          ? new NomenclatureType
                                            {
                                                Name = actObj.Province.Name
                                            }
                                          : new NomenclatureType(),
                                      Municipality = actObj?.Municipality?.Name != null
                                          ? new NomenclatureType
                                            {
                                                Name = actObj.Municipality.Name
                                            }
                                          : new NomenclatureType(),
                                      Settlement = actObj?.Settlement?.Name != null
                                          ? new NomenclatureType
                                            {
                                                Name = actObj.Settlement.Name
                                            }
                                          : new NomenclatureType(),
                                      Region =
                                          actObj?.Region?.Name != null
                                              ? new NomenclatureType
                                                {
                                                    Name = actObj.Region.Name
                                                }
                                              : new NomenclatureType(),
                                      RegPlace = actObj?.RegPlace ?? string.Empty,
                                      RegQuarter = actObj?.RegQuarter ?? string.Empty,
                                      RegUpi = actObj?.RegUpi ?? string.Empty,
                                      CadIdentifier = actObj?.CadIdentifier ?? string.Empty,
                                      CadPlanRegion = actObj?.CadPlanRegion ?? string.Empty,
                                      CadPlanNumber = actObj?.CadPlanNumber ?? string.Empty
                                  };

                        actObject.Add(obj);
                    }

                    var admActResponse = new AmdActResponseType
                                         {
                                             AdmAct = new AdmActType
                                                      {
                                                          RegNumber = doc.RegNumber,
                                                          RegDate = doc.RegDate?.ToShortDateString(),
                                                          AnnoucmentDate =
                                                              doc?.StateUpsertModel?.AnnouncementDate
                                                                 ?.ToShortDateString() ?? string.Empty,
                                                          AnnoucmentType = new NomenclatureType
                                                                           {
                                                                               Name = doc?.StateUpsertModel
                                                                                       ?.AnnouncementType?.Name ??
                                                                                   string.Empty
                                                                           },
                                                          Type = new NomenclatureType
                                                                 {
                                                                     Name = doc?.Type?.Name ?? string.Empty
                                                                 },
                                                          State = new NomenclatureType
                                                                  {
                                                                      Name = doc?.StateUpsertModel?.State?.Name ??
                                                                             string.Empty
                                                                  },
                                                          EffectiveDate =
                                                              doc?.StateUpsertModel?.EffectiveDate
                                                                 ?.ToShortDateString() ?? string.Empty,
                                                          ValidByDate =
                                                              doc?.StateUpsertModel?.ValidByDate?.ToShortDateString() ??
                                                              string.Empty
                                                      },
                                             Applicant = applicants,
                                             AdmActObject = new AdmActObjectType
                                                            {
                                                                NameDesc = doc?.Object?.NameDesc ?? string.Empty,
                                                                ActObject = actObject
                                                            },
                                             Issuer = new IssuerType
                                                      {
                                                          Administration = doc?.Issuer?.Administration ?? string.Empty,
                                                          AdministrativeBody = new NomenclatureType
                                                                               {
                                                                                   Name = doc?.Issuer
                                                                                           ?.AdministrativeBody?.Name ??
                                                                                       string.Empty
                                                                               }
                                                      }
                                         };

                    response.AmdActResponse.Add(admActResponse);
                }
            }

            return response;
        }
    }
}
