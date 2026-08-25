namespace ZARI.Application.Features.Purchasing.Suppliers.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.Suppliers.Get;
using ZARI.Domain.Common;

public sealed record GetAllSuppliersQuery : IQuery<Result<List<SupplierResponse>>>;
