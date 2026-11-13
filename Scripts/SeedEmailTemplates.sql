-- The emails the app sends, as the Liquid templates they start from. Run against PokeBinder.db.
--
-- THIS IS NOT A MIGRATION, AND IT IS NOT RUN BY DbUp. A template is content: it is edited at
-- runtime, and a migration that inserted one would run once and be wrong the first time anybody
-- changed the wording. See "Projects for outside services" in CLAUDE.md.
--
-- RE-RUNNABLE, AND IT NEVER OVERWRITES. Each statement inserts a template only when no row has its
-- name yet, so running this on a database whose templates have been edited changes nothing. To put
-- a template back to what is written here, delete its row first.
--
-- Each template renders against its model in PokeBinder.Emails/Messages, and may only use what that
-- model has: a name it does not have is refused when the email is rendered.
--
--   sign-in   SignInEmail: email, signInLink, expiresInMinutes
--
-- Values print HTML-encoded in the html part and as they are in the subject and text.

INSERT INTO [emailTemplates] ([name], [subject], [html], [text], [updatedAt])
VALUES (
    'sign-in',
    'Your PokeBinder sign-in link',
    '<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Sign in to PokeBinder</title>
</head>
<body style="margin:0; padding:0; background-color:#f4f2f8; font-family:-apple-system, BlinkMacSystemFont, ''Segoe UI'', Roboto, Helvetica, Arial, sans-serif; color:#1f1a2e;">
<table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="background-color:#f4f2f8;">
  <tr>
    <td align="center" style="padding:40px 16px;">
      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="max-width:480px; background-color:#ffffff; border-radius:16px; border:1px solid #e6e1f0;">
        <tr>
          <td style="padding:32px 32px 0 32px; font-size:20px; font-weight:700; color:#5b21b6;">PokeBinder</td>
        </tr>
        <tr>
          <td style="padding:16px 32px 0 32px;">
            <h1 style="margin:0; font-size:22px; line-height:1.3; color:#1f1a2e;">Sign in to PokeBinder</h1>
            <p style="margin:12px 0 0 0; font-size:15px; line-height:1.6; color:#4a4458;">Use the button below to sign in as {{ email }}. The link works once, for the next {{ expiresInMinutes }} minutes.</p>
          </td>
        </tr>
        <tr>
          <td style="padding:28px 32px;">
            <table role="presentation" cellpadding="0" cellspacing="0" border="0">
              <tr>
                <td style="border-radius:10px; background-color:#6d28d9;">
                  <a href="{{ signInLink }}" style="display:inline-block; padding:14px 28px; font-size:15px; font-weight:600; color:#ffffff; text-decoration:none; border-radius:10px;">Sign in</a>
                </td>
              </tr>
            </table>
          </td>
        </tr>
        <tr>
          <td style="padding:0 32px 32px 32px; font-size:13px; line-height:1.6; color:#6b6578;">
            <p style="margin:0;">If the button does not work, paste this link into your browser:</p>
            <p style="margin:8px 0 0 0; word-break:break-all;"><a href="{{ signInLink }}" style="color:#6d28d9;">{{ signInLink }}</a></p>
            <p style="margin:20px 0 0 0;">If you did not ask to sign in, you can ignore this email. Nobody can sign in without the link.</p>
          </td>
        </tr>
      </table>
    </td>
  </tr>
</table>
</body>
</html>',
    'Sign in to PokeBinder as {{ email }}.

Open this link to sign in. It works once, for the next {{ expiresInMinutes }} minutes:

{{ signInLink }}

If you did not ask to sign in, you can ignore this email. Nobody can sign in without the link.',
    CAST(strftime('%s', 'now') AS INTEGER)
)
ON CONFLICT ([name]) DO NOTHING;
