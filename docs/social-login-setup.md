# Social login setup — Google & Facebook

This guide walks through registering Google and Facebook OAuth clients so the
"Tiếp tục với Google" and "Tiếp tục với Facebook" buttons on `LoginPage` work
end-to-end. The Microsoft path is already wired (MSAL + Azure AD); only the
two new providers need credentials.

What's already in the code:
- Mobile: `GoogleAuthHelper`, `FacebookAuthHelper`, `LoginPageViewModel`
  commands, `LoginPage.xaml` buttons.
- Backend: ABP boilerplate already understands `provider = "Google"` and
  `provider = "Facebook"` on `IUserLoginManager`; settings live under
  `Authentication.Google` and `Authentication.Facebook` in
  `aspnet-core/src/LotteryDetection.Web.Host/appsettings.json`.

You need to:
1. Create OAuth clients in each provider's console.
2. Drop the resulting IDs into mobile + backend appsettings.
3. Replace two placeholder strings in `Platforms/iOS/Info.plist`.
4. Redeploy backend (Cloud Run) and reinstall the iOS app.

---

## 1. Google

### Create the OAuth client
1. Open https://console.cloud.google.com/apis/credentials for the
   `project-8b8163f6-9c04-4225-8f5` project (FamilyAI).
2. **Configure OAuth consent screen** (External) if you haven't yet —
   App name "DòVéSố AI", support email, developer contact, scopes
   `openid email profile`.
3. **Create Credentials → OAuth client ID**:
   - Application type: **iOS**
   - Bundle ID: `com.lotterydetection.mobile`
   - Click *Create*. Copy the `Client ID` — looks like
     `123456789-abcdefg.apps.googleusercontent.com`.
4. Repeat once for **Web application** (used by the backend to validate
   the access_token):
   - Authorized redirect URIs: not needed for this flow (the mobile is
     the only OAuth client; the backend just verifies tokens), but you
     may add the Cloud Run URL for completeness.

### Wire mobile
- `mobile/LotteryDetection.Mobile/appsettings.json`:
  ```json
  "Google": { "ClientId": "123456789-abcdefg.apps.googleusercontent.com" }
  ```
- `mobile/LotteryDetection.Mobile/Platforms/iOS/Info.plist`: replace
  `REPLACE_WITH_REVERSED_GOOGLE_CLIENT_ID` with the *reversed* client
  ID — i.e. the part before `.apps.googleusercontent.com`. Example:
  ```
  com.googleusercontent.apps.123456789-abcdefg
  ```

### Wire backend
- `aspnet-core/src/LotteryDetection.Web.Host/appsettings.json`:
  ```json
  "Authentication": {
    "Google": {
      "IsEnabled": "true",
      "ClientId": "<the iOS client ID above>",
      "ClientSecret": "<empty for iOS clients — leave as \"\"\">"
    }
  }
  ```
  Or set as env vars on Cloud Run:
  `Authentication__Google__IsEnabled=true`,
  `Authentication__Google__ClientId=...`.

---

## 2. Facebook

### Create the app
1. Open https://developers.facebook.com/apps → **Create App**.
2. Use case: **Authenticate and request data from users with Facebook
   Login**.
3. App type: **Consumer**. Name "DòVéSố AI".
4. Add the **Facebook Login for iOS** product.
   - Bundle ID: `com.lotterydetection.mobile`
   - Single Sign-On: ON
5. Note the **App ID** (numeric, e.g. `987654321`).

### Wire mobile
- `mobile/LotteryDetection.Mobile/appsettings.json`:
  ```json
  "Facebook": { "AppId": "987654321" }
  ```
- `mobile/LotteryDetection.Mobile/Platforms/iOS/Info.plist`: replace
  `REPLACE_WITH_FACEBOOK_APP_ID` with the numeric app ID — the entry
  ends up like `fb987654321`.

### Wire backend
- `aspnet-core/src/LotteryDetection.Web.Host/appsettings.json`:
  ```json
  "Authentication": {
    "Facebook": {
      "IsEnabled": "true",
      "AppId": "987654321",
      "AppSecret": "<from Facebook App → Settings → Basic>"
    }
  }
  ```
  Or env vars: `Authentication__Facebook__IsEnabled=true`,
  `Authentication__Facebook__AppId=...`,
  `Authentication__Facebook__AppSecret=...`.

---

## 3. Test

1. Push your changes (mobile config + backend config).
2. Backend: GitHub Actions auto-deploys; or run a manual
   `gcloud run services update lottery-detection-api --update-env-vars=...`.
3. Mobile: `bash mobile/scripts/install-ios-device.sh` to reinstall on
   the connected iPhone.
4. On the login screen, tap **Tiếp tục với Google** or **Tiếp tục với
   Facebook**. iOS opens an in-app ASWebAuthenticationSession sheet;
   complete sign-in; the app should land on Dashboard with the
   provider's display name pulled from the id-token (Google) or
   Graph `/me` (Facebook).

If sign-in returns 401 from the backend, double-check that
`Authentication:<Provider>:IsEnabled` is actually `"true"` (string) on
the running Cloud Run revision — env vars only take effect on the next
revision.

---

## How the flow works (reference)

```
[iOS app]                               [Provider]                [Backend]
   │
   │ tap "Tiếp tục với Google"
   ├──► WebAuthenticator.AuthenticateAsync(
   │      https://accounts.google.com/o/oauth2/v2/auth?...code+PKCE...
   │      callback = com.googleusercontent.apps.<id>:/oauth2redirect)
   │                                          │
   │                                  user signs in
   │                                          │
   │◄── 302 callback ─────────────────────────┘
   │      code=AUTHCODE
   │
   ├──► POST oauth2.googleapis.com/token
   │      grant_type=authorization_code, code, code_verifier
   │◄── access_token + id_token ──────────────┘
   │
   ├──► POST /api/TokenAuth/ExternalAuthenticate
   │      { authProvider: "Google",
   │        providerKey:    "<sub from id_token>",
   │        providerAccessCode: "<access_token>" }
   │                                                  ┌──────────────┐
   │                                                  │ ABP validates│
   │                                                  │ access_token │
   │                                                  │ vs Google's  │
   │                                                  │ userinfo,    │
   │                                                  │ creates/updates
   │                                                  │ User, signs  │
   │                                                  │ JWT          │
   │                                                  └──────────────┘
   │◄── ABP bearer ──────────────────────────────────────────────┘
   │
   ▼
Dashboard
```

Facebook is the same shape, except the WebAuthenticator response carries
the `access_token` directly (implicit flow) and `providerKey` comes from
`graph.facebook.com/me?fields=id,name,email`.
