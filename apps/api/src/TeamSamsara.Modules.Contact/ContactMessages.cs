/* File : /team-samsara/apps/api/src/TeamSamsara.Modules.Contact/ContactMessages.cs
   Version : 1.0.0
   Latest commit: feat/contact-module
   Author : Gerrah
   Purpose : Subject, formats and HTML template of the staff notification email sent for each contact form submission. */

namespace TeamSamsara.Modules.Contact;

internal static class ContactMessages
{
    public const string CategoryKey = "category";
    public const string NameKey = "name";
    public const string EmailKey = "email";
    public const string PhoneKey = "phone";
    public const string PhoneRowKey = "phoneRow";
    public const string ReceivedKey = "received";
    public const string MessageKey = "message";

    public const string SubjectFormat = "New contact form: {0} - {1}";
    public const string ReceivedFormat = "yyyy-MM-dd HH:mm 'UTC'";

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

    private const string PageStyle =
        "margin:0;padding:0;background-color:" + ColorPage + ";font-family:" + FontBody + ";";
    private const string ContainerStyle = "max-width:600px;margin:0 auto;padding:20px;";
    private const string HeaderStyle =
        "background-color:" + ColorHeader + ";color:" + ColorWhite + ";padding:30px;text-align:center;"
        + "border-radius:8px 8px 0 0;";
    private const string HeadingStyle =
        "margin:0;font-family:" + FontHeading + ";font-size:28px;font-weight:400;text-transform:uppercase;";
    private const string ContentStyle =
        "background-color:" + ColorContent + ";padding:40px 30px;border-radius:0 0 8px 8px;";
    private const string InfoBoxStyle =
        "background-color:" + ColorWhite + ";padding:20px;margin:0 0 20px;border-radius:4px;"
        + "border:1px solid " + ColorBorder + ";";
    private const string LabelStyle =
        "margin:0 0 5px;font-size:14px;font-weight:600;color:" + ColorLabel + ";";
    private const string ValueStyle =
        "margin:0 0 15px;font-size:16px;color:" + ColorText + ";word-wrap:break-word;";
    private const string MessageBoxStyle =
        "background-color:" + ColorWhite + ";padding:20px;border-left:4px solid " + ColorAccent + ";"
        + "border-radius:4px;";
    private const string MessageTextStyle =
        "margin:0;font-size:16px;line-height:1.6;color:" + ColorText + ";white-space:pre-wrap;word-wrap:break-word;";
    private const string FooterStyle =
        "text-align:center;font-size:12px;color:" + ColorFooter + ";padding:20px 0 0;margin-top:20px;"
        + "border-top:1px solid " + ColorFooterBorder + ";";
    private const string FooterLineStyle = "margin:5px 0;";

    public const string PhoneRow =
        "<div style=\"" + LabelStyle + "\">Phone</div>"
        + "<div style=\"" + ValueStyle + "\">{{" + PhoneKey + "}}</div>";

    public const string Body =
        "<!DOCTYPE html>"
        + "<html><head><meta charset=\"UTF-8\"></head>"
        + "<body style=\"" + PageStyle + "\">"
        + "<div style=\"" + ContainerStyle + "\">"
        + "<div style=\"" + HeaderStyle + "\">"
        + "<h1 style=\"" + HeadingStyle + "\">Website Contact Form</h1>"
        + "</div>"
        + "<div style=\"" + ContentStyle + "\">"
        + "<div style=\"" + InfoBoxStyle + "\">"
        + "<div style=\"" + LabelStyle + "\">From</div>"
        + "<div style=\"" + ValueStyle + "\">{{" + NameKey + "}}</div>"
        + "<div style=\"" + LabelStyle + "\">Email</div>"
        + "<div style=\"" + ValueStyle + "\">{{" + EmailKey + "}}</div>"
        + "{{" + PhoneRowKey + "}}"
        + "<div style=\"" + LabelStyle + "\">Subject</div>"
        + "<div style=\"" + ValueStyle + "\">{{" + CategoryKey + "}}</div>"
        + "<div style=\"" + LabelStyle + "\">Received</div>"
        + "<div style=\"" + ValueStyle + "\">{{" + ReceivedKey + "}}</div>"
        + "</div>"
        + "<div style=\"" + MessageBoxStyle + "\">"
        + "<p style=\"" + MessageTextStyle + "\">{{" + MessageKey + "}}</p>"
        + "</div>"
        + "</div>"
        + "<div style=\"" + FooterStyle + "\">"
        + "<p style=\"" + FooterLineStyle + "\">This is an automated notification from the Samsara website contact form.</p>"
        + "<p style=\"" + FooterLineStyle + "\">Reply directly to this email to respond to the sender.</p>"
        + "</div>"
        + "</div></body></html>";
}
