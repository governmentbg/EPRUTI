namespace Ais.Office.Areas.Admin.Controllers
{
    using System;

    using Ais.Infrastructure.Roles;
    using Ais.Office.ViewModels.Logs;
    using Ais.Table.Mvc.Controllers;
    using Ais.Table.Mvc.Utilities;
    using Ais.Utilities.Exception;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.SessionStorage;
    using AutoMapper;
    using Elasticsearch.Net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Nest;

    using LogLevel = Microsoft.Extensions.Logging.LogLevel;

    /// <summary>
    /// Class LogsController.
    /// Implements the <see cref="LogTableViewModel" />
    /// </summary>
    /// <seealso cref="LogTableViewModel" />
    [Area("Admin")]
    [Authorize(Roles = UserRolesConstants.LogsRead)]
    public class LogsController : SearchTableController<LogQueryViewModel, LogTableViewModel>
    {
        private readonly IElasticClient elasticClient;
        private readonly IMapper mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogsController"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="sessionStorageService">The session storage service.</param>
        public LogsController(
            ILogger<LogsController> logger,
            IStringLocalizer localizer,
            IMapper mapper,
            IConfiguration configuration,
            ISessionStorageService sessionStorageService)
            : base(logger, localizer, sessionStorageService)
        {
            this.mapper = mapper;
            var urls = configuration.GetSection("ElasticSearch:Uri").Get<string[]>().Select(url => new Uri(url)).ToArray();
            var settings = (urls.Length > 1 ? new ConnectionSettings(new StaticConnectionPool(urls)) : new ConnectionSettings(urls[0]))
                           .DefaultIndex(configuration.GetValue<string>("ElasticSearch:log-index"))
                           .DefaultFieldNameInferrer(s => s)
                           .BasicAuthentication(configuration.GetValue<string>("ElasticSearch:User"), configuration.GetValue<string>("ElasticSearch:Password"));
            this.elasticClient = new ElasticClient(settings);
            this.Options.TableHeaderText = localizer["Logs"];
            this.Options.ShowFieldToolTip = false;
            this.Options.Breadcrumbs = new[] { new Ais.Data.Models.Breadcrumb { Title = this.Localizer["Admin"] } };
        }

        public override Task<IActionResult> Index(LogQueryViewModel query = null)
        {
            if (query == null || !ReflectionUtils.HasNonNullProperty(query))
            {
                query = new LogQueryViewModel
                {
                    StartDate = DateTime.Now.Date,
                    EndDate = DateTime.Now.AddDays(1).Date,
                    Size = 200,
                    Level = Enum.GetName(LogLevel.Error)
                };
            }

            return base.Index(query);
        }

        /// <summary>
        /// Informations the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="searchQueryId">The search query identifier.</param>
        /// <returns>IActionResult.</returns>
        /// <exception cref="Ais.Utilities.Exception.UserException"></exception>
        [HttpGet]
        public async Task<IActionResult> Info(Guid id, string searchQueryId)
        {
            var queryModel = await this.GetQueryModelAsync(searchQueryId);
            queryModel.CorrelationId = id.ToString();
            queryModel.Level = null;
            queryModel.Size = 50; //// with null it gets only 10 records by default, and sometimes when we have many DbCalls, we may lose some ot the log data due to bigger number of records

            var response = await this.elasticClient.SearchAsync(this.GetSearchQuery(queryModel));

            if (response.Documents.IsNullOrEmpty())
            {
                throw new UserException(this.Localizer["DataNotFound"]);
            }

            var result = this.mapper.Map<LogViewModel>(response.Documents);

            return this.PartialView(result);
        }

        /// <summary>
        /// Find results as an asynchronous operation.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A Task&lt;IEnumerable`1&gt; representing the asynchronous operation.</returns>
        protected override async Task<IEnumerable<LogTableViewModel>> FindResultsAsync(LogQueryViewModel query)
        {
            var result = await this.elasticClient.SearchAsync(this.GetSearchQuery(query));

            return this.mapper.Map<List<LogTableViewModel>>(result.Documents.DistinctBy(x => x.Properties.CorrelationId));
        }

        /// <summary>
        /// Initials the query asynchronous.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>Task.</returns>
        protected override Task InitialQueryAsync(LogQueryViewModel model)
        {
            model.LevelDataSource = Enum.GetNames(typeof(LogLevel)).Select(item => new KeyValuePair<string, string>(item, item)).ToList().AddDropDownAll();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the search query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Func&lt;SearchDescriptor&lt;LogData&gt;, ISearchRequest&gt;.</returns>
        private Func<SearchDescriptor<LogData>, ISearchRequest> GetSearchQuery(LogQueryViewModel query)
        {
            return searchDescriptor => searchDescriptor
                        .Query(
                            q =>
                                   q.DateRange(r => r.Field(f => f.Timestamp).GreaterThan(query.StartDate?.ToUniversalTime()).LessThan(query.EndDate?.ToUniversalTime())) &&
                                   q.MatchPhrase(m => m.Field(x => x.Properties.CorrelationId).Query(query.CorrelationId)) &&
                                   q.MatchPhrase(m => m.Field(x => x.Properties.ClientIp).Query(query.ClientIp)) &&
                                   q.MatchPhrase(m => m.Field(x => x.Properties.UserId).Query(query.UserId)) &&
                                   q.MatchPhrase(m => m.Field(x => x.Properties.UserName).Query(query.UserName)) &&
                                   q.MatchPhrase(m => m.Field(x => x.Properties.UserKnik).Query(query.UserKnik)) &&
                                   q.MatchPhrase(m => m.Field(x => x.Properties.Module).Query(query.Module)) &&
                                   q.MatchPhrase(m => m.Field(x => x.Server).Query(query.Server)) &&
                                   q.MatchPhrase(m => m.Field(x => x.Level).Query(query.Level)))
                        .Size(query.Size)
                        .Sort(sort => sort.Descending(f => f.Timestamp));
        }
    }
}
