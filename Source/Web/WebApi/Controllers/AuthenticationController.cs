namespace WebApi.Controllers
{
    using Ais.Utilities.Extensions;

    using Ais.WebServices.Models.Authentication;
    using Ais.WebServices.Services.Authentication;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    using WebApi.Model.Authentication;

    [AllowAnonymous]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService authenticationService;

        public AuthenticationController(IAuthenticationService authenticationService)
        {
            this.authenticationService = authenticationService;
        }

        /// <summary>
        /// Authenticate user.
        /// </summary>
        /// <param name="model">Username and password for authentication.</param>
        /// <returns>AccessToken and refresh token.</returns>
        [HttpPost("[action]")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TokenResponse>> SignIn([FromBody] SignInRequest model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            var token = await this.authenticationService.SingInAsync(model.UserName, model.Password);
            if (token?.AccessData?.Token.IsNotNullOrEmpty() != true)
            {
                return this.BadRequest("Invalid authentication! Check if the user exists or the credentials are valid or the user is active!");
            }

            return token;
        }

        /// <summary>
        /// Generate new access token.
        /// </summary>
        /// <param name="model">Refresh token to generate new access token.</param>
        /// <returns>AccessToken and refresh token.</returns>
        [HttpPost("[action]")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TokenResponse>> Refresh([FromBody] RefreshTokenRequest model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            var token = await this.authenticationService.RefreshAsync(model.RefreshToken);
            if (token?.AccessData?.Token.IsNotNullOrEmpty() != true)
            {
                return this.BadRequest($"Invalid {nameof(model.RefreshToken)}!");
            }

            return token;
        }
    }
}
