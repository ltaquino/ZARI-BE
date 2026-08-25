namespace ZARI.Application.Features.Purchasing.Suppliers.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteSupplierCommand(Guid Id) : ICommand;
