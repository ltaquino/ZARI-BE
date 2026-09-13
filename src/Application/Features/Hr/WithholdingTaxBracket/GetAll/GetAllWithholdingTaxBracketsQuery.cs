namespace ZARI.Application.Features.Hr.WithholdingTaxBrackets.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.WithholdingTaxBrackets.Get;
using ZARI.Domain.Common;

public sealed record GetAllWithholdingTaxBracketsQuery : IQuery<Result<List<WithholdingTaxBracketResponse>>>;
