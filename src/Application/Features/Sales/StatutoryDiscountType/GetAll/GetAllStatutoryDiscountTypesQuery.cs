namespace ZARI.Application.Features.Sales.StatutoryDiscountTypes.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.StatutoryDiscountTypes.Get;
using ZARI.Domain.Common;

public sealed record GetAllStatutoryDiscountTypesQuery : IQuery<Result<List<StatutoryDiscountTypeResponse>>>;
