using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RetailPointBackend.Models;

namespace RetailPointBackend.Services
{
    public interface IEInvoiceService
    {
        Task<EInvoiceApiResponse> IssueInvoiceAsync(EInvoice invoice, EInvoiceConfig config);
        Task<EInvoiceApiResponse> CancelInvoiceAsync(EInvoice invoice, EInvoiceConfig config, string reason);
        Task<EInvoiceApiResponse> GetInvoiceStatusAsync(string transactionUuid, EInvoiceConfig config);
        
        // PortalService APIs
        Task<string> DownloadInv(string invToken, string username, string password);
        Task<string> DownloadInvNoPay(string invToken, string username, string password);
        Task<string> DownloadInvFkey(string fkey, string username, string password);
        Task<string> DownloadInvFkeyNoPay(string fkey, string username, string password);
        Task<string> DownloadNewInvPDFFkey(string fkey, string username, string password);
        Task<string> DownloadInvPDF(string invToken, string username, string password);
        Task<string> DownloadInvPDFFkeyNoPay(string fkey, string username, string password);
        Task<string> ListInvFromNoToNo(string invFromNo, string invToNo, string invPattern, string invSerial, string username, string password);
        Task<string> ListInvByCus(string cusCode, string fromDate, string toDate, string username, string password);
        Task<string> ListInvByCusFkey(string fkey, string fromDate, string toDate, string username, string password);
        Task<string> GetInvView(string invToken, string username, string password);
        Task<string> GetInvViewNoPay(string invToken, string username, string password);
        Task<string> GetInvViewFkey(string fkey, string username, string password);
        Task<string> GetNewInvViewFkey(string fkey, string username, string password);
        Task<string> GetInvViewFkeyNoPay(string fkey, string username, string password);
        Task<string> ConvertForVerify(string invToken, string username, string password);
        Task<string> ConvertForVerifyFkey(string fkey, string username, string password);
        Task<string> ConvertForStore(string invToken, string username, string password);
        Task<string> GetCus(string cusCode, string username, string password);
        Task<string> ConvertForStoreFkey(string fkey, string username, string password);
        Task<string> DownloadInvErrorFkey(string fkey, string username, string password);
        Task<string> DownloadInvErrorPDF(string token, string username, string password);
        Task<string> GetInvErrorViewFkey(string fkey, string username, string password);
        Task<string> DownloadInvPDFFkeyError(string invToken, string username, string password);
        Task<string> DownloadInvError(string invToken, string username, string password);
        Task<string> DownloadInvNoPayError(string invToken, string username, string password);
        Task<string> DownloadInvPDFFkeyNoPayError(string fkey, string username, string password);
        Task<string> DownloadInvPDFNoPayError(string token, string username, string password);
        Task<string> GetInvViewByDate(string username, string password, string pattern, string serial, string fromDate, string toDate);
        Task<string> DownloadInvZipFkey(string fkey, string username, string password, bool checkPayment);
        Task<string> DownloadInvZipToken(string invToken, string username, string password, bool checkPayment);
        
        // MTT (Máy tính tiền) APIs
        Task<string> ImportAndPublishInvMTT(string account, string acPass, string xmlInvData, string username, string password, string pattern, string serial, int convert = 0);
        Task<string> ImportInvByPatternMTT(string account, string acPass, string xmlInvData, string username, string password, string pattern, string serial, int convert = 0);
        Task<string> SendInvMTT(string account, string acPass, string username, string password, string pattern, string serial, string fkey);
    }

    public class EInvoiceService : IEInvoiceService
    {
        private readonly HttpClient _httpClient;

        public EInvoiceService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<EInvoiceApiResponse> IssueInvoiceAsync(EInvoice invoice, EInvoiceConfig config)
        {
            try
            {
                var apiRequest = CreateIssueRequest(invoice, config);
                var requestJson = JsonSerializer.Serialize(apiRequest);
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                // Thêm header xác thực nếu cần
                if (!string.IsNullOrEmpty(config.Token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.Token);
                }

                var response = await _httpClient.PostAsync($"{config.ApiUrl}/invoice/issue", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<EInvoiceApiResponse>(responseContent);
                    return apiResponse ?? new EInvoiceApiResponse 
                    { 
                        Success = false, 
                        ErrorMessage = "Không thể parse response từ API" 
                    };
                }
                else
                {
                    return new EInvoiceApiResponse
                    {
                        Success = false,
                        ErrorMessage = $"API Error: {response.StatusCode} - {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new EInvoiceApiResponse
                {
                    Success = false,
                    ErrorMessage = $"Lỗi kết nối API: {ex.Message}"
                };
            }
        }

        public async Task<EInvoiceApiResponse> CancelInvoiceAsync(EInvoice invoice, EInvoiceConfig config, string reason)
        {
            try
            {
                var apiRequest = new
                {
                    transactionUuid = invoice.TransactionUuid,
                    reason = reason,
                    companyCode = config.CompanyCode
                };

                var requestJson = JsonSerializer.Serialize(apiRequest);
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                if (!string.IsNullOrEmpty(config.Token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.Token);
                }

                var response = await _httpClient.PostAsync($"{config.ApiUrl}/invoice/cancel", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<EInvoiceApiResponse>(responseContent);
                    return apiResponse ?? new EInvoiceApiResponse 
                    { 
                        Success = false, 
                        ErrorMessage = "Không thể parse response từ API" 
                    };
                }
                else
                {
                    return new EInvoiceApiResponse
                    {
                        Success = false,
                        ErrorMessage = $"API Error: {response.StatusCode} - {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new EInvoiceApiResponse
                {
                    Success = false,
                    ErrorMessage = $"Lỗi kết nối API: {ex.Message}"
                };
            }
        }

        public async Task<EInvoiceApiResponse> GetInvoiceStatusAsync(string transactionUuid, EInvoiceConfig config)
        {
            try
            {
                if (!string.IsNullOrEmpty(config.Token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.Token);
                }

                var response = await _httpClient.GetAsync($"{config.ApiUrl}/invoice/status/{transactionUuid}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonSerializer.Deserialize<EInvoiceApiResponse>(responseContent);
                    return apiResponse ?? new EInvoiceApiResponse 
                    { 
                        Success = false, 
                        ErrorMessage = "Không thể parse response từ API" 
                    };
                }
                else
                {
                    return new EInvoiceApiResponse
                    {
                        Success = false,
                        ErrorMessage = $"API Error: {response.StatusCode} - {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                return new EInvoiceApiResponse
                {
                    Success = false,
                    ErrorMessage = $"Lỗi kết nối API: {ex.Message}"
                };
            }
        }

        private object CreateIssueRequest(EInvoice invoice, EInvoiceConfig config)
        {
            return new
            {
                companyCode = config.CompanyCode,
                invoice = new
                {
                    templateCode = invoice.InvoiceTemplate,
                    invoiceSymbol = invoice.InvoiceSymbol,
                    invoiceNumber = invoice.InvoiceNumber,
                    issueDate = invoice.IssueDate.ToString("yyyy-MM-dd"),
                    currencyCode = invoice.CurrencyCode,
                    exchangeRate = invoice.ExchangeRate,
                    
                    // Thông tin người bán
                    seller = new
                    {
                        taxCode = invoice.SellerTaxCode,
                        name = invoice.SellerName,
                        address = invoice.SellerAddress,
                        phone = invoice.SellerPhone,
                        email = invoice.SellerEmail,
                        bankAccount = invoice.SellerBankAccount,
                        bankName = invoice.SellerBankName
                    },
                    
                    // Thông tin người mua
                    buyer = new
                    {
                        taxCode = invoice.BuyerTaxCode,
                        name = invoice.BuyerName,
                        address = invoice.BuyerAddress,
                        phone = invoice.BuyerPhone,
                        email = invoice.BuyerEmail
                    },
                    
                    // Chi tiết hóa đơn
                    items = invoice.Items.Select(item => new
                    {
                        lineNumber = item.LineNumber,
                        itemCode = item.ItemCode,
                        itemName = item.ItemName,
                        unit = item.Unit,
                        quantity = item.Quantity,
                        unitPrice = item.UnitPrice,
                        lineTotal = item.LineTotal,
                        taxRate = item.TaxRate,
                        taxAmount = item.TaxAmount,
                        totalAmount = item.TotalAmount,
                        discountRate = item.DiscountRate,
                        discountAmount = item.DiscountAmount
                    }).ToArray(),
                    
                    // Tổng tiền
                    summary = new
                    {
                        subTotal = invoice.SubTotal,
                        taxAmount = invoice.TaxAmount,
                        totalAmount = invoice.TotalAmount,
                        discountAmount = invoice.DiscountAmount
                    },
                    
                    // Thông tin bổ sung
                    paymentMethod = invoice.PaymentMethod,
                    notes = invoice.Notes
                }
            };
        }

        // PortalService API Methods - tra cứu, download, chuyển đổi, báo cáo
        public async Task<string> DownloadInv(string invToken, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> DownloadInvNoPay(string invToken, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> DownloadInvFkey(string fkey, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> DownloadInvFkeyNoPay(string fkey, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> DownloadNewInvPDFFkey(string fkey, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> DownloadInvPDF(string invToken, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> DownloadInvPDFFkeyNoPay(string fkey, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> ListInvFromNoToNo(string invFromNo, string invToNo, string invPattern, string invSerial, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> ListInvByCus(string cusCode, string fromDate, string toDate, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> ListInvByCusFkey(string fkey, string fromDate, string toDate, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> GetInvView(string invToken, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> GetInvViewNoPay(string invToken, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> GetInvViewFkey(string fkey, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> GetNewInvViewFkey(string fkey, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> GetInvViewFkeyNoPay(string fkey, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> ConvertForVerify(string invToken, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> ConvertForVerifyFkey(string fkey, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> ConvertForStore(string invToken, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> GetCus(string cusCode, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> ConvertForStoreFkey(string fkey, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> DownloadInvErrorFkey(string fkey, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> DownloadInvErrorPDF(string token, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> GetInvErrorViewFkey(string fkey, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> DownloadInvPDFFkeyError(string invToken, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> DownloadInvError(string invToken, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> DownloadInvNoPayError(string invToken, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> DownloadInvPDFFkeyNoPayError(string fkey, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> DownloadInvPDFNoPayError(string token, string userName, string userPass)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> GetInvViewByDate(string userName, string userPass, string pattern, string serial, string fromDate, string toDate)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> DownloadInvZipFkey(string fkey, string userName, string userPass, bool checkPayment)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> DownloadInvZipToken(string invToken, string userName, string userPass, bool checkPayment)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        // MTT (Máy tính tiền) APIs
        public async Task<string> ImportAndPublishInvMTT(string account, string acPass, string xmlInvData, 
            string username, string password, string pattern, string serial, int convert = 0)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> ImportInvByPatternMTT(string account, string acPass, string xmlInvData, 
            string username, string password, string pattern, string serial, int convert = 0)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }

        public async Task<string> SendInvMTT(string account, string acPass, string username, string password, 
            string pattern, string serial, string fkey)
        {
            return await Task.FromResult("Chưa triển khai API thực tế");
        }
    }

    public class EInvoiceApiResponse
    {
        public bool Success { get; set; }
        public string? TransactionUuid { get; set; }
        public string? InvoiceAuthCode { get; set; }
        public string? InvoiceUrl { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ErrorCode { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public object? Data { get; set; }
    }

    // Mock service cho testing
    public class MockEInvoiceService : IEInvoiceService
    {
        public async Task<EInvoiceApiResponse> IssueInvoiceAsync(EInvoice invoice, EInvoiceConfig config)
        {
            // Giả lập delay API
            await Task.Delay(1000);
            
            return new EInvoiceApiResponse
            {
                Success = true,
                TransactionUuid = Guid.NewGuid().ToString(),
                InvoiceAuthCode = $"AUTH{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}",
                InvoiceUrl = $"https://mock-einvoice.com/view/{invoice.InvoiceNumber}",
                ProcessedAt = DateTime.Now
            };
        }

        public async Task<EInvoiceApiResponse> CancelInvoiceAsync(EInvoice invoice, EInvoiceConfig config, string reason)
        {
            await Task.Delay(500);
            
            return new EInvoiceApiResponse
            {
                Success = true,
                TransactionUuid = invoice.TransactionUuid,
                ProcessedAt = DateTime.Now
            };
        }

        public async Task<EInvoiceApiResponse> GetInvoiceStatusAsync(string transactionUuid, EInvoiceConfig config)
        {
            await Task.Delay(300);
            
            return new EInvoiceApiResponse
            {
                Success = true,
                TransactionUuid = transactionUuid,
                Data = new { status = "issued", issuedAt = DateTime.Now },
                ProcessedAt = DateTime.Now
            };
        }

        // PortalService API Methods - Mock implementations
        public async Task<string> DownloadInv(string invToken, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock invoice data - DownloadInv";
        }

        public async Task<string> DownloadInvNoPay(string invToken, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock invoice data - DownloadInvNoPay";
        }

        public async Task<string> DownloadInvFkey(string fkey, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock invoice data - DownloadInvFkey";
        }

        public async Task<string> DownloadInvFkeyNoPay(string fkey, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock invoice data - DownloadInvFkeyNoPay";
        }

        public async Task<string> DownloadNewInvPDFFkey(string fkey, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock PDF data - DownloadNewInvPDFFkey";
        }

        public async Task<string> DownloadInvPDF(string invToken, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock PDF data - DownloadInvPDF";
        }

        public async Task<string> DownloadInvPDFFkeyNoPay(string fkey, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock PDF data - DownloadInvPDFFkeyNoPay";
        }

        public async Task<string> ListInvFromNoToNo(string invFromNo, string invToNo, string invPattern, string invSerial, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock invoice list - ListInvFromNoToNo";
        }

        public async Task<string> ListInvByCus(string cusCode, string fromDate, string toDate, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock invoice list - ListInvByCus";
        }

        public async Task<string> ListInvByCusFkey(string fkey, string fromDate, string toDate, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock invoice list - ListInvByCusFkey";
        }

        public async Task<string> GetInvView(string invToken, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock invoice view - GetInvView";
        }

        public async Task<string> GetInvViewNoPay(string invToken, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock invoice view - GetInvViewNoPay";
        }

        public async Task<string> GetInvViewFkey(string fkey, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock invoice view - GetInvViewFkey";
        }

        public async Task<string> GetNewInvViewFkey(string fkey, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock invoice view - GetNewInvViewFkey";
        }

        public async Task<string> GetInvViewFkeyNoPay(string fkey, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock invoice view - GetInvViewFkeyNoPay";
        }

        public async Task<string> ConvertForVerify(string invToken, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock convert result - ConvertForVerify";
        }

        public async Task<string> ConvertForVerifyFkey(string fkey, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock convert result - ConvertForVerifyFkey";
        }

        public async Task<string> ConvertForStore(string invToken, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock convert result - ConvertForStore";
        }

        public async Task<string> GetCus(string cusCode, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock customer data - GetCus";
        }

        public async Task<string> ConvertForStoreFkey(string fkey, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock convert result - ConvertForStoreFkey";
        }

        public async Task<string> DownloadInvErrorFkey(string fkey, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock error invoice - DownloadInvErrorFkey";
        }

        public async Task<string> DownloadInvErrorPDF(string token, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock error PDF - DownloadInvErrorPDF";
        }

        public async Task<string> GetInvErrorViewFkey(string fkey, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock error view - GetInvErrorViewFkey";
        }

        public async Task<string> DownloadInvPDFFkeyError(string invToken, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock error PDF - DownloadInvPDFFkeyError";
        }

        public async Task<string> DownloadInvError(string invToken, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock error invoice - DownloadInvError";
        }

        public async Task<string> DownloadInvNoPayError(string invToken, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock error invoice - DownloadInvNoPayError";
        }

        public async Task<string> DownloadInvPDFFkeyNoPayError(string fkey, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock error PDF - DownloadInvPDFFkeyNoPayError";
        }

        public async Task<string> DownloadInvPDFNoPayError(string token, string userName, string userPass)
        {
            await Task.Delay(100);
            return "Mock error PDF - DownloadInvPDFNoPayError";
        }

        public async Task<string> GetInvViewByDate(string userName, string userPass, string pattern, string serial, string fromDate, string toDate)
        {
            await Task.Delay(100);
            return "Mock invoice list by date - GetInvViewByDate";
        }

        public async Task<string> DownloadInvZipFkey(string fkey, string userName, string userPass, bool checkPayment)
        {
            await Task.Delay(100);
            return "Mock ZIP data - DownloadInvZipFkey";
        }

        public async Task<string> DownloadInvZipToken(string invToken, string userName, string userPass, bool checkPayment)
        {
            await Task.Delay(100);
            return "Mock ZIP data - DownloadInvZipToken";
        }

        // MTT (Máy tính tiền) APIs
        public async Task<string> ImportAndPublishInvMTT(string account, string acPass, string xmlInvData, 
            string username, string password, string pattern, string serial, int convert = 0)
        {
            await Task.Delay(100);
            return "Mock response - ImportAndPublishInvMTT";
        }

        public async Task<string> ImportInvByPatternMTT(string account, string acPass, string xmlInvData, 
            string username, string password, string pattern, string serial, int convert = 0)
        {
            await Task.Delay(100);
            return "Mock response - ImportInvByPatternMTT";
        }

        public async Task<string> SendInvMTT(string account, string acPass, string username, string password, 
            string pattern, string serial, string fkey)
        {
            await Task.Delay(100);
            return "Mock response - SendInvMTT";
        }
    }
}