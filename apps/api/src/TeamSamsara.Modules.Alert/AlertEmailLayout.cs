// File : /team-samsara/apps/api/src/TeamSamsara.Modules.Alert/AlertEmailLayout.cs
// Version : 1.0.0
// Latest commit: task/web-account-security
// Author : Gerrah
// Purpose : The shared HTML layout of every alert email: dark header, light body with a title, an intro, an optional code card and details card, a note and a footer.

using System.Text;
using TeamSamsara.Shared.Alert;

namespace TeamSamsara.Modules.Alert;

internal static class AlertEmailLayout
{
    #region Fields

    private const string ColorHeader = "#151d23";
    private const string ColorAccent = "#95ff50";
    private const string ColorContent = "#f2ffeb";
    private const string ColorPage = "#f9f9f9";
    private const string ColorBorder = "#dddddd";
    private const string ColorLabel = "#666666";
    private const string ColorText = "#000000";
    private const string ColorFooter = "#999999";
    private const string ColorFooterBorder = "#eeeeee";
    private const string ColorWhite = "#ffffff";

    private const string FontBody = "Arial,Helvetica,sans-serif";
    private const string FontHeading = "Anton,Impact,Arial,sans-serif";

    private const string ContentWidthPx = "600";

    private const string TableAttributes =
        "role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"";

    private const string PageStyle =
        "margin:0;padding:0;background-color:" + ColorPage + ";font-family:" + FontBody + ";";
    private const string OuterCellStyle = "padding:20px;";
    private const string InnerTableStyle = "width:100%;max-width:" + ContentWidthPx + "px;";
    private const string HeaderStyle =
        "background-color:" + ColorHeader + ";color:" + ColorWhite + ";padding:30px;text-align:center;"
        + "border-radius:8px 8px 0 0;";
    private const string WordmarkStyle =
        "margin:0;font-family:" + FontHeading + ";font-size:28px;font-weight:400;text-transform:uppercase;"
        + "letter-spacing:0.02em;";
    private const string ContentStyle =
        "background-color:" + ColorContent + ";padding:36px 30px;border-radius:0 0 8px 8px;";
    private const string TitleStyle =
        "margin:0 0 12px;font-size:22px;line-height:1.25;color:" + ColorText + ";";
    private const string IntroStyle =
        "margin:0 0 24px;font-size:16px;line-height:1.6;color:" + ColorText + ";";
    private const string CardStyle =
        "background-color:" + ColorWhite + ";padding:18px 20px 4px;margin:0 0 20px;border-radius:4px;"
        + "border:1px solid " + ColorBorder + ";";
    private const string CodeCardStyle =
        "background-color:" + ColorWhite + ";padding:22px 16px;margin:0 0 20px;border-radius:4px;"
        + "border:1px solid " + ColorBorder + ";border-left:4px solid " + ColorAccent + ";text-align:center;";
    private const string CodeLabelStyle =
        "margin:0;font-size:13px;font-weight:600;color:" + ColorLabel + ";text-transform:uppercase;"
        + "letter-spacing:0.08em;";
    private const string CodeStyle =
        "margin:8px 0;font-family:" + FontHeading + ";font-size:46px;font-weight:400;line-height:1.1;"
        + "letter-spacing:10px;color:" + ColorText + ";";
    private const string CodeExpiryStyle = "margin:0;font-size:13px;color:" + ColorLabel + ";";
    private const string LabelStyle =
        "margin:0 0 4px;font-size:13px;font-weight:600;color:" + ColorLabel + ";";
    private const string ValueStyle =
        "margin:0 0 14px;font-size:16px;color:" + ColorText + ";word-wrap:break-word;";
    private const string NoteStyle =
        "margin:0;font-size:14px;line-height:1.6;color:" + ColorLabel + ";";
    private const string FooterStyle =
        "text-align:center;font-size:12px;color:" + ColorFooter + ";padding:20px 0 0;margin-top:20px;"
        + "border-top:1px solid " + ColorFooterBorder + ";";
    private const string FooterLineStyle = "margin:5px 0;";

    private const string CodeToken = "{{" + AlertTemplateKeys.Code + "}}";
    private const string ExpiryToken = "{{" + AlertTemplateKeys.ExpiresInMinutes + "}}";

    private const string NeverShareCode = " Never share this code: Team Samsara will never ask you for it.";

    #endregion

    #region Public Methods

    // Builds the full HTML, leaving {{token}} placeholders for the sender to fill with encoded values
    public static string Render(
        string title,
        string intro,
        string note,
        bool showCode = false,
        IReadOnlyList<(string Label, string Value)>? details = null)
    {
        var html = new StringBuilder();

        html.Append("<!DOCTYPE html><html><head><meta charset=\"UTF-8\">")
            .Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"></head>")
            .Append("<body style=\"").Append(PageStyle).Append("\">")
            .Append("<table ").Append(TableAttributes).Append(" width=\"100%\" style=\"background-color:")
            .Append(ColorPage).Append(";\"><tr><td align=\"center\" style=\"").Append(OuterCellStyle).Append("\">")
            .Append("<table ").Append(TableAttributes).Append(" width=\"").Append(ContentWidthPx)
            .Append("\" style=\"").Append(InnerTableStyle).Append("\"><tr><td align=\"left\" style=\"text-align:left;\">")
            .Append("<div style=\"").Append(HeaderStyle).Append("\"><h1 style=\"").Append(WordmarkStyle)
            .Append("\">Team Samsara</h1></div>")
            .Append("<div style=\"").Append(ContentStyle).Append("\">")
            .Append("<h2 style=\"").Append(TitleStyle).Append("\">").Append(title).Append("</h2>")
            .Append("<p style=\"").Append(IntroStyle).Append("\">").Append(intro).Append("</p>");

        if (showCode)
        {
            AppendCodeCard(html);
        }

        if (details is { Count: > 0 })
        {
            AppendDetailsCard(html, details);
        }

        html.Append("<p style=\"").Append(NoteStyle).Append("\">").Append(note)
            .Append(showCode ? NeverShareCode : string.Empty).Append("</p>")
            .Append("</div>")
            .Append("<div style=\"").Append(FooterStyle).Append("\">")
            .Append("<p style=\"").Append(FooterLineStyle).Append("\">This is an automated message from Team Samsara.</p>")
            .Append("<p style=\"").Append(FooterLineStyle).Append("\">Please do not reply to this email.</p>")
            .Append("</div></td></tr></table></td></tr></table></body></html>");

        return html.ToString();
    }

    #endregion

    #region Private Methods

    // The large one-time code with its expiry
    private static void AppendCodeCard(StringBuilder html)
    {
        html.Append("<div style=\"").Append(CodeCardStyle).Append("\">")
            .Append("<p style=\"").Append(CodeLabelStyle).Append("\">Your code</p>")
            .Append("<p style=\"").Append(CodeStyle).Append("\">").Append(CodeToken).Append("</p>")
            .Append("<p style=\"").Append(CodeExpiryStyle).Append("\">Expires in ").Append(ExpiryToken)
            .Append(" minutes</p></div>");
    }

    // A white card of label and value rows
    private static void AppendDetailsCard(StringBuilder html, IReadOnlyList<(string Label, string Value)> details)
    {
        html.Append("<div style=\"").Append(CardStyle).Append("\">");

        foreach (var (label, value) in details)
        {
            html.Append("<div style=\"").Append(LabelStyle).Append("\">").Append(label).Append("</div>")
                .Append("<div style=\"").Append(ValueStyle).Append("\">").Append(value).Append("</div>");
        }

        html.Append("</div>");
    }

    #endregion
}
