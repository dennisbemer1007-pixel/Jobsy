"""Faithful Python port of today's EmailLayout.Wrap + ApplicationConfirmation (origin/acceptatie @ a611db40), for a before/after."""
def current_confirmation(src="../src"):
    Pearl, Navy, Teal, Sky, Text, Muted = "#f5f2ee", "#0f2d5c", "#1a7a6d", "#e8eef7", "#142033", "#5a6a7d"
    subject = "Sollicitatie bevestigd: Weekendhulp verkoop"
    inner = f"""<h1 style="margin:0 0 14px 0;font-size:22px;line-height:1.3;color:{Navy};font-weight:700;">Sollicitatie verstuurd!</h1>
<p style="margin:0 0 14px 0;">Hoi Alex de Tester,</p>
<p style="margin:0 0 14px 0;">Je sollicitatie op <strong>Weekendhulp verkoop</strong> bij Bakkerij De Gouden Korrel is ontvangen. Top!</p>
<p style="margin:0 0 14px 0;">Je kunt de status volgen onder Mijn sollicitaties.</p>
<table role="presentation" cellspacing="0" cellpadding="0" style="margin:18px 0 8px 0;"><tr><td style="border-radius:10px;background:{Navy};">
<a href="https://lobsy.nl/candidate/applications" style="display:inline-block;padding:12px 22px;font-size:14px;font-weight:700;color:#ffffff;text-decoration:none;border-radius:10px;">Bekijk mijn sollicitaties</a></td></tr></table>"""
    head = '<meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1" /><title>Lobsy</title>'
    body = f"""<div style="display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;">{subject}</div>
<table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:{Pearl};padding:24px 12px;"><tr><td align="center">
<table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:560px;background:#ffffff;border-radius:16px;overflow:hidden;border:1px solid #e4e0d8;">
<tr><td style="background:{Navy};padding:22px 28px;"><table role="presentation" width="100%" cellspacing="0" cellpadding="0"><tr>
<td width="64" valign="middle" style="width:64px;"><table role="presentation" cellspacing="0" cellpadding="0"><tr><td style="background:#ffffff;border-radius:12px;padding:6px;line-height:0;">
<img src="{src}/lobsy-128.png" width="48" height="48" alt="Lobsy" border="0" style="display:block;border:0;outline:none;text-decoration:none;background:transparent;" /></td></tr></table></td>
<td valign="middle" style="padding-left:12px;"><div style="font-size:20px;font-weight:700;color:#ffffff;letter-spacing:0.02em;">Lobsy</div>
<div style="font-size:12px;color:{Sky};margin-top:2px;">Hyper-lokaal matchen</div></td></tr></table></td></tr>
<tr><td style="height:4px;background:{Teal};font-size:0;line-height:0;">&nbsp;</td></tr>
<tr><td style="padding:28px 28px 8px 28px;font-size:15px;line-height:1.55;color:{Text};">{inner}</td></tr>
<tr><td style="padding:8px 28px 28px 28px;font-size:12px;line-height:1.45;color:{Muted};">Met vriendelijke groet,<br/><strong style="color:{Navy};">Team Lobsy</strong></td></tr>
</table><p style="margin:16px 0 0;font-size:11px;color:{Muted};">Je ontvangt deze e-mail omdat je een Lobsy-account hebt of een actie op het platform hebt gedaan.</p>
</td></tr></table>"""
    return subject, subject, head, f'<div style="font-family:-apple-system,BlinkMacSystemFont,\'Segoe UI\',Roboto,Helvetica,Arial,sans-serif;color:{Text};">{body}</div>'
