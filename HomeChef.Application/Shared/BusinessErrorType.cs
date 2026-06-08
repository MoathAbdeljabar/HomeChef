namespace HomeChef.Application.Shared;
    public enum BusinessErrorType
    {
        None = 0,
        EmailAlreadyExists,
        PhoneNumberAlreadyExists,
        CanNotCreateUser,
        UserAlreadyExists,
        UserNotFound,
        InvalidToken,
        InvalidCredentials,
        InsufficientPermissions,
        ResourceNotFound,
        ValidationFailed,
        OperationNotAllowed,
        PaymentRequired,
        SubscriptionExpired,
        AuthenticatorSetupFailed,
        EmailNotConfirmed,
        AccountLocked,
        AccountDisabled,
        ConcurrencyConflict,
        PhoneNotConfirmed,
        Unknown,



        //----------------------------------------------
        InvalidImage,

}

