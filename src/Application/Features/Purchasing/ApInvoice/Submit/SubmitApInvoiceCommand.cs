namespace ZARI.Application.Features.Purchasing.ApInvoices.Submit;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Purchasing.ApInvoices.GetAll;
using ZARI.Domain.Common;

public sealed record SubmitApInvoiceCommand(Guid Id, string RequestedBy) : ICommand<Result<ApInvoiceResponse>>;
