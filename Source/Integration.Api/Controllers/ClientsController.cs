namespace Integration.Api.Controllers
{
    using System.ComponentModel.DataAnnotations;

    using Ais.Data.Base.Ais;
    using Ais.Data.Common.Base;
    using Ais.Data.Models.Base;
    using Ais.Data.Models.Client;
    using Ais.Data.Models.Journal;
    using Ais.Data.Models.QueryModels;
    using Ais.Services.Ais;

    using AutoMapper;

    using Integration.Api.Models;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [Authorize]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ClientsController : BaseController
    {
        private readonly IClientService clientService;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IMapper mapper;

        public ClientsController(
            IMapper mapper,
            IDataBaseContextManager<AisDbType> contextManager,
            IClientService clientService)
        {
            this.mapper = mapper;
            this.contextManager = contextManager;
            this.clientService = clientService;
        }

        /// <summary>
        ///     Get client data by number.
        /// </summary>
        /// <param name="number">Client number - EGN/Bulstat/RegisterNumber.</param>
        /// <returns>Client data.</returns>
        [HttpGet("[action]/{number}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Client>> FindByNumberAsync([FromRoute] [Required] string number)
        {
            Client client;
            await using (await this.contextManager.NewConnectionAsync())
            {
                client = await this.clientService.ClientLoginAsync(egnBulstat: number);
            }

            return client == null ? null : this.mapper.Map<Client>(client);
        }

        /// <summary> Upsert the client</summary>
        /// <param name="model">The model.</param>
        /// <returns>IActionResult.</returns>
        [HttpPost("UpsertClient")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Client>> Upsert(ClientUpsertModel model)
        {
            // When there is one address,and it is not the default, make it the default
            if (model.Addresses?.Count == 1 && !model.Addresses[0].Default)
            {
                model.Addresses[0].Default = true;
            }

            // Try to validate model
            this.ModelState.Clear();
            if (!this.TryValidateModel(model))
            {
                return this.BadRequest("Invalid model");
            }

            var dbClient = this.mapper.Map<Client>(model);
            var actionType = dbClient.IsNew ? ActionType.Create : ActionType.Edit;

            await using var dbConnection = await this.contextManager.NewConnectionWithJournalAsync(
                actionType,
                objects: new[] { new KeyValuePair<object, ObjectType>(dbClient, ObjectType.Client) });
            await using var dbTransaction = await dbConnection.BeginTransactionAsync();
            await this.clientService.UpsertAsync(dbClient, true);

            await dbTransaction.CommitAsync();

            var dbModel = await this.clientService.GetAsync(dbClient!.Id!.Value, false, false);
            var searchResultModel =
                (await this.clientService.SearchAsync(new ClientQueryModel { Id = dbClient.Id }))?.First()!;

            return this.Ok(dbModel);
        }
    }
}
