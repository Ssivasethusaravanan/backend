namespace identity_service.Services;

/// <summary>
/// HTML email templates for BillFlow branded transactional emails.
/// </summary>
public static class EmailTemplates
{
    public static string SignInLink(string signInLink) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>Sign in to BillFlow</title>
        </head>
        <body style="margin:0;padding:0;background:#f0f4f8;font-family:'Segoe UI',Helvetica,Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" role="presentation">
            <tr>
              <td align="center" style="padding:40px 16px;">
                <table width="100%" cellpadding="0" cellspacing="0" role="presentation"
                       style="max-width:480px;background:#ffffff;border-radius:12px;
                              box-shadow:0 4px 24px rgba(0,0,0,.08);">
        
                  <!-- Header -->
                  <tr>
                    <td align="center" style="padding:36px 40px 24px;">
                      <div style="display:inline-flex;align-items:center;justify-content:center;
                                  width:52px;height:52px;background:linear-gradient(135deg,#6366f1,#8b5cf6);
                                  border-radius:12px;margin-bottom:16px;">
                        <span style="font-size:26px;">🧾</span>
                      </div>
                      <h1 style="margin:0;font-size:22px;font-weight:700;color:#1e293b;letter-spacing:-.4px;">
                        BillFlow
                      </h1>
                    </td>
                  </tr>
        
                  <!-- Body -->
                  <tr>
                    <td style="padding:0 40px;">
                      <h2 style="margin:0 0 8px;font-size:18px;font-weight:600;color:#1e293b;">
                        Sign in to your account
                      </h2>
                      <p style="margin:0 0 24px;font-size:14px;color:#64748b;line-height:1.6;">
                        Click the button below to securely sign in to BillFlow. This link expires in
                        <strong>1 hour</strong> and can only be used once.
                      </p>
                      <a href="{signInLink}"
                         style="display:block;text-align:center;background:linear-gradient(135deg,#6366f1,#8b5cf6);
                                color:#ffffff;font-size:15px;font-weight:600;text-decoration:none;
                                padding:14px 28px;border-radius:8px;letter-spacing:.2px;">
                        Sign in to BillFlow
                      </a>
                      <p style="margin:24px 0 0;font-size:12px;color:#94a3b8;line-height:1.5;">
                        If you didn't request this email, you can safely ignore it. Someone may have
                        typed your email address by mistake.
                      </p>
                    </td>
                  </tr>
        
                  <!-- Footer -->
                  <tr>
                    <td align="center"
                        style="padding:28px 40px;border-top:1px solid #e2e8f0;margin-top:24px;">
                      <p style="margin:0;font-size:12px;color:#94a3b8;">
                        © {DateTime.UtcNow.Year} BillFlow · Smart Billing Platform
                      </p>
                    </td>
                  </tr>
        
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;

    public static string Welcome(string userName) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8" />
          <title>Welcome to BillFlow</title>
        </head>
        <body style="margin:0;padding:0;background:#f0f4f8;font-family:'Segoe UI',Helvetica,Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" role="presentation">
            <tr>
              <td align="center" style="padding:40px 16px;">
                <table width="100%" cellpadding="0" cellspacing="0" role="presentation"
                       style="max-width:480px;background:#ffffff;border-radius:12px;
                              box-shadow:0 4px 24px rgba(0,0,0,.08);">
                  <tr>
                    <td style="padding:36px 40px;">
                      <h1 style="margin:0 0 8px;font-size:22px;font-weight:700;color:#1e293b;">
                        Welcome to BillFlow, {userName}! 🎉
                      </h1>
                      <p style="margin:0;font-size:14px;color:#64748b;line-height:1.6;">
                        Your account is all set. Start managing bills, tracking orders, and growing
                        your business — all in one place.
                      </p>
                    </td>
                  </tr>
                </table>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;
}
