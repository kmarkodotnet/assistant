using FamilyOs.Application.Search.Dtos;
using FamilyOs.Application.Search.Handlers;
using FamilyOs.Application.Search.Intent;
using MediatR;

namespace FamilyOs.Application.Search;

public sealed class SearchHandler : IRequestHandler<SearchCommand, SearchResponse>
{
    private readonly FilterSearchHandler _filterHandler;
    private readonly FtsSearchHandler _ftsHandler;
    private readonly SemanticSearchHandler _semanticHandler;
    private readonly HybridSearchHandler _hybridHandler;
    private readonly QaHandler _qaHandler;
    private readonly CommandHandler _commandHandler;

    public SearchHandler(
        FilterSearchHandler filterHandler,
        FtsSearchHandler ftsHandler,
        SemanticSearchHandler semanticHandler,
        HybridSearchHandler hybridHandler,
        QaHandler qaHandler,
        CommandHandler commandHandler)
    {
        _filterHandler = filterHandler;
        _ftsHandler = ftsHandler;
        _semanticHandler = semanticHandler;
        _hybridHandler = hybridHandler;
        _qaHandler = qaHandler;
        _commandHandler = commandHandler;
    }

    public async Task<SearchResponse> Handle(SearchCommand command, CancellationToken cancellationToken)
    {
        var req = command.Request;
        var userId = command.UserId;

        var mode = req.Mode;

        if (mode == SearchMode.Auto)
            mode = ClassifyMode(req.Query);

        return mode switch
        {
            SearchMode.Filter => await _filterHandler.SearchAsync(req, userId, cancellationToken),
            SearchMode.Text => await _ftsHandler.SearchAsync(req, userId, cancellationToken),
            SearchMode.Semantic => await _semanticHandler.SearchAsync(req, userId, cancellationToken),
            SearchMode.Qa => await _qaHandler.SearchAsync(req, userId, cancellationToken),
            SearchMode.Command => await _commandHandler.SearchAsync(
                req, userId, command.UserFamilyMemberId, command.UserRole, cancellationToken),
            _ => await _hybridHandler.SearchAsync(req, userId, cancellationToken),
        };
    }

    private static SearchMode ClassifyMode(string query)
    {
        var (intent, confidence) = IntentClassifier.Classify(query);

        return intent switch
        {
            SearchIntent.Filter => SearchMode.Filter,
            SearchIntent.Lookup => SearchMode.Text,
            SearchIntent.Summarize => SearchMode.Qa,
            SearchIntent.Aggregate => SearchMode.Qa,
            SearchIntent.Find when confidence >= 0.7 => SearchMode.Semantic,
            _ => SearchMode.Semantic, // low confidence → semantic (hybrid internally)
        };
    }
}
