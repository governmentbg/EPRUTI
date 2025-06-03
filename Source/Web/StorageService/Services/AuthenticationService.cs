namespace StorageService.Services
{
    using Ais.Common.Context;
    using Ais.Utilities.Extensions;
    using Ais.WebServices.Services.Authentication;

    using global::StorageService.AuthenticationGrpc;
    using global::StorageService.Models;

    using Google.Protobuf.WellKnownTypes;

    using Grpc.Core;

    public class AuthenticationService : Authentication.AuthenticationBase
    {
        private readonly IAuthenticationService authenticationService;
        private readonly RequestContext requestContext;

        public AuthenticationService(IAuthenticationService authenticationService, IRequestContext requestContext)
        {
            this.authenticationService = authenticationService;
            this.requestContext = requestContext as RequestContext;
        }

        public override async Task<TokenResponse> SignIn(SignInRequest request, ServerCallContext context)
        {
            this.requestContext?.Init(context);
            var tokenData = await this.authenticationService.SingInAsync(request.Username, request.Password);
            if (tokenData?.AccessData?.Token.IsNotNullOrEmpty() != true)
            {
                throw new RpcException(new Status(StatusCode.Unknown, "Invalid authentication! Check if the user exists or the credentials are valid or the user is active!"));
            }

            return ToTokenResponse(tokenData);
        }

        public override async Task<TokenResponse> Refresh(RefreshRequest request, ServerCallContext context)
        {
            this.requestContext?.Init(context);
            var tokenData = await this.authenticationService.RefreshAsync(request.Token);
            if (tokenData?.AccessData?.Token.IsNotNullOrEmpty() != true)
            {
                throw new RpcException(new Status(StatusCode.Unknown, "Invalid refresh token or user is blocked!"));
            }

            return ToTokenResponse(tokenData);
        }

        private static TokenResponse ToTokenResponse(Ais.WebServices.Models.Authentication.TokenResponse tokenData)
        {
            return new TokenResponse
            {
                AccessData = new TokenData
                {
                    Token = tokenData!.AccessData!.Token,
                    ValidTo = tokenData!.AccessData!.ValidTo.ToTimestamp(),
                },
                RefreshData = new TokenData
                {
                    Token = tokenData!.RefreshData!.Token,
                    ValidTo = tokenData!.RefreshData!.ValidTo.ToTimestamp(),
                },
            };
        }
    }
}
