namespace Portfolio.Application.Models;

/// <summary>
/// One published page read from an external content source (today: Notion).
/// </summary>
/// <remarks>
/// Plan 3.2 / KI-045. This is not a domain concept. The portfolio model knows about
/// roles, projects, stacks and recommendations, and about no vendor at all; a page of
/// markdown from somebody else's CMS is an integration concern. It lives in Application
/// because the use case needs a shape for the content it reads, and because the domain
/// should not own an inbound port it never calls.
///
/// <para>
/// The fields stay <c>string</c> on purpose. They are untrusted upstream values that are
/// published verbatim; parsing them into <c>Uri</c>/<c>DateOnly</c> here would turn a page
/// whose date reads "Spring 2024" into a <c>502</c>.
/// </para>
/// </remarks>
public sealed record PageContent(string Url, string Date, string Title);
