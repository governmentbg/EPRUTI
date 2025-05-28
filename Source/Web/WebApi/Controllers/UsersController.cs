namespace WebApi.Controllers
{
    using System.ComponentModel.DataAnnotations;

    using Ais.Services.Ais;
    using Ais.Utilities.Extensions;

    using AutoMapper;

    using global::Ais.Data.Base.Ais;
    using global::Ais.Data.Common.Base;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    using WebApi.Infrastructure;
    using WebApi.Model.User;

    [Authorize]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IMapper mapper;
        private readonly IDataBaseContextManager<AisDbType> contextManager;
        private readonly IUserService userService;

        public UsersController(IMapper mapper, IDataBaseContextManager<AisDbType> contextManager, IUserService userService)
        {
            this.mapper = mapper;
            this.contextManager = contextManager;
            this.userService = userService;
        }

        /// <summary>
        /// Get user data by id.
        /// </summary>
        /// <param name="id">User identifier.</param>
        /// <returns>User with roles and activities.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SkipUserId]
        public async Task<ActionResult<User>> GetAsync([FromRoute] Guid id)
        {
            Ais.Data.Models.User.User user;
            await using (await this.contextManager.NewConnectionAsync())
            {
                user = await this.userService.LoginAsync(userId: id);
            }

            return user == null ? null : this.mapper.Map<User>(user);
        }

        /// <summary>
        /// Get user data by username.
        /// </summary>
        /// <param name="userName">Username.</param>
        /// <returns>User with roles and activities.</returns>
        [HttpGet("GetByUserName/{userName}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [SkipUserId]
        public async Task<ActionResult<User>> GetByUserNameAsync([FromRoute][StringLength(100)] string userName)
        {
            if (userName.IsNullOrEmpty())
            {
                return this.BadRequest($"Parameter '{nameof(userName)}' is required!");
            }

            Ais.Data.Models.User.User user;
            await using (await this.contextManager.NewConnectionAsync())
            {
                user = await this.userService.LoginAsync(userName: userName);
            }

            return user == null ? null : this.mapper.Map<User>(user);
        }

        /// <summary>
        /// Get user roles and activities.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <returns>Collection with user roles and activities.</returns>
        [HttpGet("Roles/{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [SkipUserId]
        public async Task<ActionResult<List<Role>>> GetRolesAsync([FromRoute] Guid userId)
        {
            List<Ais.Data.Models.User.Role> roles;
            await using (await this.contextManager.NewConnectionAsync())
            {
                roles = await this.userService.GetRolesAsync(userId);
            }

            return this.mapper.Map<List<Role>>(roles ?? new List<Ais.Data.Models.User.Role>());
        }

        /// <summary>
        /// Get user roles and activities by EGN.
        /// </summary>
        /// <param name="egn">User EGN.</param>
        /// <returns>Collection with user roles and activities.</returns>
        [HttpGet("GetByEgn/{egn}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [SkipUserId]
        public async Task<ActionResult<ByEgnSearchResult>> GetByEgnAsync([FromRoute] string egn)
        {
            Ais.Data.Models.User.ApiSearchResult result;
            await using (await this.contextManager.NewConnectionAsync())
            {
                result = await this.userService.GetApiUserByEgnAsync(egn);
            }

            return this.mapper.Map<ByEgnSearchResult>(result);
        }
    }
}
