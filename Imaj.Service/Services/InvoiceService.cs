using Imaj.Core.Entities;
using Imaj.Core.Interfaces.Repositories;
using Imaj.Service.DTOs;
using Imaj.Service.Interfaces;
using Imaj.Service.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Imaj.Service.Services
{
    public class InvoiceService : BaseService, IInvoiceService
    {
        public InvoiceService(IUnitOfWork unitOfWork, ILogger<InvoiceService> logger, IConfiguration configuration)
            : base(unitOfWork, logger, configuration)
        {
        }

        public async Task<ServiceResult<PagedResult<InvoiceDto>>> GetByFilterAsync(InvoiceFilterDto filter)
        {
            try
            {
                var invoices = _unitOfWork.Repository<Invoice>().Query();
                var customers = _unitOfWork.Repository<Customer>().Query();
                var states = _unitOfWork.Repository<XState>().Query();

                var query = from inv in invoices
                            join jobCust in customers on inv.JobCustomerID equals jobCust.Id into jobCustGroup
                            from jobCustomer in jobCustGroup.DefaultIfEmpty()
                            join invoCust in customers on inv.InvoCustomerID equals invoCust.Id into invoCustGroup
                            from invoiceCustomer in invoCustGroup.DefaultIfEmpty()
                            join xstate in states.Where(s => s.LanguageID == CurrentLanguageId)
                                on inv.StateID equals xstate.StateID into stateGroup
                            from state in stateGroup.DefaultIfEmpty()
                            select new
                            {
                                Invoice = inv,
                                JobCustomer = jobCustomer,
                                InvoiceCustomer = invoiceCustomer,
                                StateName = state != null ? state.Name : null
                            };

                // Filters
                if (!string.IsNullOrWhiteSpace(filter.JobCustomerCode))
                {
                    var code = filter.JobCustomerCode.Trim();
                    query = query.Where(x => x.JobCustomer != null && x.JobCustomer.Code == code);
                }

                if (!string.IsNullOrWhiteSpace(filter.JobCustomerName))
                {
                    var name = filter.JobCustomerName.Trim();
                    query = query.Where(x => x.JobCustomer != null && x.JobCustomer.Name.Contains(name));
                }

                if (!string.IsNullOrWhiteSpace(filter.InvoiceCustomerCode))
                {
                    var code = filter.InvoiceCustomerCode.Trim();
                    query = query.Where(x => x.InvoiceCustomer != null && x.InvoiceCustomer.Code == code);
                }

                if (!string.IsNullOrWhiteSpace(filter.InvoiceCustomerName))
                {
                    var name = filter.InvoiceCustomerName.Trim();
                    query = query.Where(x => x.InvoiceCustomer != null && x.InvoiceCustomer.Name.Contains(name));
                }

                if (filter.ReferenceStart.HasValue)
                {
                    query = query.Where(x => x.Invoice.Reference >= filter.ReferenceStart.Value);
                }

                if (filter.ReferenceEnd.HasValue)
                {
                    query = query.Where(x => x.Invoice.Reference <= filter.ReferenceEnd.Value);
                }

                if (!string.IsNullOrWhiteSpace(filter.Name))
                {
                    var name = filter.Name.Trim();
                    query = query.Where(x => x.Invoice.Name.Contains(name));
                }

                if (!string.IsNullOrWhiteSpace(filter.RelatedPerson))
                {
                    var related = filter.RelatedPerson.Trim();
                    query = query.Where(x => x.Invoice.Contact.Contains(related));
                }

                if (filter.IssueDateStart.HasValue)
                {
                    query = query.Where(x => x.Invoice.IssueDate >= filter.IssueDateStart.Value);
                }

                if (filter.IssueDateEnd.HasValue)
                {
                    query = query.Where(x => x.Invoice.IssueDate <= filter.IssueDateEnd.Value);
                }

                if (filter.StateId.HasValue)
                {
                    query = query.Where(x => x.Invoice.StateID == filter.StateId.Value);
                }

                if (filter.Evaluated.HasValue)
                {
                    query = query.Where(x => x.Invoice.Evaluated == filter.Evaluated.Value);
                }

                var totalCount = await query.CountAsync();

                var page = filter.Page > 0 ? filter.Page : 1;
                var pageSize = filter.PageSize > 0 ? filter.PageSize : 10;
                var skip = (page - 1) * pageSize;

                var items = await query
                    .OrderByDescending(x => x.Invoice.IssueDate)
                    .ThenByDescending(x => x.Invoice.Id)
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(x => new InvoiceDto
                    {
                        Id = x.Invoice.Id,
                        Reference = x.Invoice.Reference,
                        JobCustomerCode = x.JobCustomer != null ? x.JobCustomer.Code : null,
                        JobCustomerName = x.JobCustomer != null ? x.JobCustomer.Name : null,
                        InvoiceCustomerCode = x.InvoiceCustomer != null ? x.InvoiceCustomer.Code : null,
                        InvoiceCustomerName = x.InvoiceCustomer != null ? x.InvoiceCustomer.Name : null,
                        Name = x.Invoice.Name,
                        IssueDate = x.Invoice.IssueDate,
                        GrossAmount = x.Invoice.GrossAmount,
                        StateName = x.StateName,
                        Evaluated = x.Invoice.Evaluated
                    })
                    .ToListAsync();

                var result = new PagedResult<InvoiceDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = page,
                    PageSize = pageSize
                };

                return ServiceResult<PagedResult<InvoiceDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invoice search failed");
                return ServiceResult<PagedResult<InvoiceDto>>.Fail("Fatura sorgulama sırasında hata oluştu.");
            }
        }
    }
}
