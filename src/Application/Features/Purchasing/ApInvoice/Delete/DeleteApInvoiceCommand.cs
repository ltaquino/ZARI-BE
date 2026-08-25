namespace ZARI.Application.Features.Purchasing.ApInvoices.Delete;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

public sealed record DeleteApInvoiceCommand(Guid Id) : ICommand<Result>;
