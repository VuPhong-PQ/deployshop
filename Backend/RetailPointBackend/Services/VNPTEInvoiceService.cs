using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using RetailPointBackend.Models;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace RetailPointBackend.Services
{
    public class VNPTEInvoiceService : IEInvoiceService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<VNPTEInvoiceService> _logger;
        private readonly AppDbContext _context;

        public VNPTEInvoiceService(HttpClient httpClient, ILogger<VNPTEInvoiceService> logger, AppDbContext context)
        {
            _httpClient = httpClient;
            _logger = logger;
            _context = context;
        }

        public async Task<EInvoiceApiResponse> IssueInvoiceAsync(EInvoice invoice, EInvoiceConfig config)
        {
            try
            {
                // Tạo XML data theo format VNPT
                var xmlInvData = CreateVNPTXmlInvoice(invoice, config);
                
                _logger.LogInformation($"VNPT XML Invoice Data: {xmlInvData}");

                // Gọi API ImportAndPublishInv của VNPT
                var response = await CallVNPTImportAndPublishInv(
                    account: config.Username ?? "",
                    acPass: config.Password ?? "",
                    xmlInvData: xmlInvData,
                    username: config.Username ?? "",
                    password: config.Password ?? "",
                    pattern: invoice.InvoiceTemplate,
                    serial: invoice.InvoiceSymbol,
                    config: config
                );

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling VNPT ImportAndPublishInv API");
                return new EInvoiceApiResponse
                {
                    Success = false,
                    ErrorMessage = $"Lỗi kết nối VNPT API: {ex.Message}"
                };
            }
        }

        public async Task<EInvoiceApiResponse> CancelInvoiceAsync(EInvoice invoice, EInvoiceConfig config, string reason)
        {
            try
            {
                // VNPT sử dụng deleteInvoiceByFkey để hủy hóa đơn
                if (string.IsNullOrEmpty(invoice.TransactionUuid))
                {
                    return new EInvoiceApiResponse
                    {
                        Success = false,
                        ErrorMessage = "Không có Fkey để hủy hóa đơn"
                    };
                }

                var response = await CallVNPTDeleteInvoiceByFkey(
                    lstFkey: invoice.TransactionUuid,
                    username: config.Username ?? "",
                    password: config.Password ?? "",
                    account: config.Username ?? "",
                    acPass: config.Password ?? "",
                    config: config
                );

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling VNPT deleteInvoiceByFkey API");
                return new EInvoiceApiResponse
                {
                    Success = false,
                    ErrorMessage = $"Lỗi hủy hóa đơn VNPT: {ex.Message}"
                };
            }
        }

        public async Task<EInvoiceApiResponse> GetInvoiceStatusAsync(string transactionUuid, EInvoiceConfig config)
        {
            try
            {
                // VNPT không có API riêng để get status, có thể sử dụng GetCertInfo để test connection
                var response = await CallVNPTGetCertInfo(
                    username: config.Username ?? "",
                    password: config.Password ?? "",
                    config: config
                );

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling VNPT GetCertInfo API");
                return new EInvoiceApiResponse
                {
                    Success = false,
                    ErrorMessage = $"Lỗi kiểm tra trạng thái VNPT: {ex.Message}"
                };
            }
        }

        #region VNPT API Calls

        private async Task<EInvoiceApiResponse> CallVNPTImportAndPublishInv(
            string account, string acPass, string xmlInvData, 
            string username, string password, string pattern, 
            string serial, EInvoiceConfig config)
        {
            try
            {
                // Tạo SOAP request cho VNPT
                var soapEnvelope = CreateSOAPRequest("ImportAndPublishInv", new Dictionary<string, object>
                {
                    { "Account", account },
                    { "ACpass", acPass },
                    { "xmlInvData", xmlInvData },
                    { "username", username },
                    { "password", password },
                    { "pattern", pattern },
                    { "serial", serial },
                    { "convert", 0 }
                });

                var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
                content.Headers.Clear();
                content.Headers.Add("Content-Type", "text/xml; charset=utf-8");
                content.Headers.Add("SOAPAction", "http://tempuri.org/ImportAndPublishInv");

                var response = await _httpClient.PostAsync($"{config.ApiUrl}/PublishService.asmx", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"VNPT ImportAndPublishInv Response: {responseContent}");

                return ParseVNPTResponse(responseContent, "ImportAndPublishInv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CallVNPTImportAndPublishInv");
                throw;
            }
        }

        private async Task<EInvoiceApiResponse> CallVNPTDeleteInvoiceByFkey(
            string lstFkey, string username, string password, 
            string account, string acPass, EInvoiceConfig config)
        {
            try
            {
                var soapEnvelope = CreateSOAPRequest("deleteInvoiceByFkey", new Dictionary<string, object>
                {
                    { "lstFkey", lstFkey },
                    { "username", username },
                    { "password", password },
                    { "Account", account },
                    { "ACpass", acPass }
                });

                var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
                content.Headers.Clear();
                content.Headers.Add("Content-Type", "text/xml; charset=utf-8");
                content.Headers.Add("SOAPAction", "http://tempuri.org/deleteInvoiceByFkey");

                var response = await _httpClient.PostAsync($"{config.ApiUrl}/PublishService.asmx", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"VNPT deleteInvoiceByFkey Response: {responseContent}");

                return ParseVNPTResponse(responseContent, "deleteInvoiceByFkey");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CallVNPTDeleteInvoiceByFkey");
                throw;
            }
        }

        private async Task<EInvoiceApiResponse> CallVNPTGetCertInfo(
            string username, string password, EInvoiceConfig config)
        {
            try
            {
                var soapEnvelope = CreateSOAPRequest("GetCertInfo", new Dictionary<string, object>
                {
                    { "userName", username },
                    { "password", password }
                });

                var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
                content.Headers.Clear();
                content.Headers.Add("Content-Type", "text/xml; charset=utf-8");
                content.Headers.Add("SOAPAction", "http://tempuri.org/GetCertInfo");

                var response = await _httpClient.PostAsync($"{config.ApiUrl}/PublishService.asmx", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"VNPT GetCertInfo Response: {responseContent}");

                return ParseVNPTResponse(responseContent, "GetCertInfo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CallVNPTGetCertInfo");
                throw;
            }
        }

        #endregion

        #region PortalService APIs - Tra cứu, Download, Chuyển đổi, Báo cáo

        public async Task<string> DownloadInv(string invToken, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("downloadInv", new Dictionary<string, string>
                {
                    { "invToken", invToken },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "downloadInv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DownloadInv");
                throw;
            }
        }

        public async Task<string> DownloadInvNoPay(string invToken, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("downloadInvNoPay", new Dictionary<string, string>
                {
                    { "invToken", invToken },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "downloadInvNoPay");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DownloadInvNoPay");
                throw;
            }
        }

        public async Task<string> DownloadInvFkey(string fkey, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("downloadInvFkey", new Dictionary<string, string>
                {
                    { "fkey", fkey },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "downloadInvFkey");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DownloadInvFkey");
                throw;
            }
        }

        public async Task<string> DownloadInvFkeyNoPay(string fkey, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("downloadInvFkeyNoPay", new Dictionary<string, string>
                {
                    { "fkey", fkey },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "downloadInvFkeyNoPay");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DownloadInvFkeyNoPay");
                throw;
            }
        }

        public async Task<string> DownloadNewInvPDFFkey(string fkey, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("downloadNewInvPDFFkey", new Dictionary<string, string>
                {
                    { "fkey", fkey },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "downloadNewInvPDFFkey");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DownloadNewInvPDFFkey");
                throw;
            }
        }

        public async Task<string> DownloadInvPDF(string invToken, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("downloadInvPDF", new Dictionary<string, string>
                {
                    { "invToken", invToken },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "downloadInvPDF");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DownloadInvPDF");
                throw;
            }
        }

        public async Task<string> DownloadInvPDFFkeyNoPay(string fkey, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("downloadInvPDFFkeyNoPay", new Dictionary<string, string>
                {
                    { "fkey", fkey },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "downloadInvPDFFkeyNoPay");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DownloadInvPDFFkeyNoPay");
                throw;
            }
        }

        public async Task<string> ListInvFromNoToNo(string invFromNo, string invToNo, string invPattern, string invSerial, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("listInvFromNoToNo", new Dictionary<string, string>
                {
                    { "invFromNo", invFromNo },
                    { "invToNo", invToNo },
                    { "invPattern", invPattern },
                    { "invSerial", invSerial },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "listInvFromNoToNo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ListInvFromNoToNo");
                throw;
            }
        }

        public async Task<string> ListInvByCus(string cusCode, string fromDate, string toDate, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("listInvByCus", new Dictionary<string, string>
                {
                    { "cusCode", cusCode },
                    { "fromDate", fromDate },
                    { "toDate", toDate },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "listInvByCus");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ListInvByCus");
                throw;
            }
        }

        public async Task<string> ListInvByCusFkey(string fkey, string fromDate, string toDate, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("listInvByCusFkey", new Dictionary<string, string>
                {
                    { "fkey", fkey },
                    { "fromDate", fromDate },
                    { "toDate", toDate },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "listInvByCusFkey");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ListInvByCusFkey");
                throw;
            }
        }

        public async Task<string> GetInvView(string invToken, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("getInvView", new Dictionary<string, string>
                {
                    { "invToken", invToken },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "getInvView");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetInvView");
                throw;
            }
        }

        public async Task<string> GetInvViewNoPay(string invToken, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("getInvViewNoPay", new Dictionary<string, string>
                {
                    { "invToken", invToken },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "getInvViewNoPay");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetInvViewNoPay");
                throw;
            }
        }

        public async Task<string> GetInvViewFkey(string fkey, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("getInvViewFkey", new Dictionary<string, string>
                {
                    { "fkey", fkey },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "getInvViewFkey");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetInvViewFkey");
                throw;
            }
        }

        public async Task<string> GetNewInvViewFkey(string fkey, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("getNewInvViewFkey", new Dictionary<string, string>
                {
                    { "fkey", fkey },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "getNewInvViewFkey");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetNewInvViewFkey");
                throw;
            }
        }

        public async Task<string> GetInvViewFkeyNoPay(string fkey, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("getInvViewFkeyNoPay", new Dictionary<string, string>
                {
                    { "fkey", fkey },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "getInvViewFkeyNoPay");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetInvViewFkeyNoPay");
                throw;
            }
        }

        public async Task<string> ConvertForVerify(string invToken, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("convertForVerify", new Dictionary<string, string>
                {
                    { "invToken", invToken },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "convertForVerify");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConvertForVerify");
                throw;
            }
        }

        public async Task<string> ConvertForVerifyFkey(string fkey, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("convertForVerifyFkey", new Dictionary<string, string>
                {
                    { "fkey", fkey },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "convertForVerifyFkey");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConvertForVerifyFkey");
                throw;
            }
        }

        public async Task<string> ConvertForStore(string invToken, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("convertForStore", new Dictionary<string, string>
                {
                    { "invToken", invToken },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "convertForStore");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConvertForStore");
                throw;
            }
        }

        public async Task<string> GetCus(string cusCode, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("getCus", new Dictionary<string, string>
                {
                    { "cusCode", cusCode },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "getCus");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCus");
                throw;
            }
        }

        public async Task<string> ConvertForStoreFkey(string fkey, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("convertForStoreFkey", new Dictionary<string, string>
                {
                    { "fkey", fkey },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "convertForStoreFkey");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConvertForStoreFkey");
                throw;
            }
        }

        public async Task<string> DownloadInvErrorFkey(string fkey, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("downloadInvErrorFkey", new Dictionary<string, string>
                {
                    { "fkey", fkey },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "downloadInvErrorFkey");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DownloadInvErrorFkey");
                throw;
            }
        }

        public async Task<string> DownloadInvErrorPDF(string token, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("downloadInvErrorPDF", new Dictionary<string, string>
                {
                    { "token", token },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "downloadInvErrorPDF");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DownloadInvErrorPDF");
                throw;
            }
        }

        public async Task<string> GetInvErrorViewFkey(string fkey, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("GetInvErrorViewFkey", new Dictionary<string, string>
                {
                    { "fkey", fkey },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "GetInvErrorViewFkey");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetInvErrorViewFkey");
                throw;
            }
        }

        public async Task<string> DownloadInvPDFFkeyError(string invToken, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("downloadInvPDFFkeyError", new Dictionary<string, string>
                {
                    { "invToken", invToken },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "downloadInvPDFFkeyError");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DownloadInvPDFFkeyError");
                throw;
            }
        }

        public async Task<string> DownloadInvError(string invToken, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("downloadInvError", new Dictionary<string, string>
                {
                    { "invToken", invToken },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "downloadInvError");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DownloadInvError");
                throw;
            }
        }

        public async Task<string> DownloadInvNoPayError(string invToken, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("downloadInvNoPayError", new Dictionary<string, string>
                {
                    { "invToken", invToken },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "downloadInvNoPayError");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DownloadInvNoPayError");
                throw;
            }
        }

        public async Task<string> DownloadInvPDFFkeyNoPayError(string fkey, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("downloadInvPDFFkeyNoPayError", new Dictionary<string, string>
                {
                    { "fkey", fkey },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "downloadInvPDFFkeyNoPayError");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DownloadInvPDFFkeyNoPayError");
                throw;
            }
        }

        public async Task<string> DownloadInvPDFNoPayError(string token, string username, string password)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("downloadInvPDFNoPayError", new Dictionary<string, string>
                {
                    { "token", token },
                    { "userName", username },
                    { "userPass", password }
                });

                return await SendSoapRequest(soapEnvelope, "downloadInvPDFNoPayError");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DownloadInvPDFNoPayError");
                throw;
            }
        }

        public async Task<string> GetInvViewByDate(string username, string password, string pattern, string serial, string fromDate, string toDate)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("GetInvViewByDate", new Dictionary<string, string>
                {
                    { "userName", username },
                    { "userPass", password },
                    { "pattern", pattern },
                    { "serial", serial },
                    { "fromDate", fromDate },
                    { "toDate", toDate }
                });

                return await SendSoapRequest(soapEnvelope, "GetInvViewByDate");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetInvViewByDate");
                throw;
            }
        }

        public async Task<string> DownloadInvZipFkey(string fkey, string username, string password, bool checkPayment)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("downloadInvZipFkey", new Dictionary<string, string>
                {
                    { "fkey", fkey },
                    { "userName", username },
                    { "userPass", password },
                    { "checkPayment", checkPayment.ToString().ToLower() }
                });

                return await SendSoapRequest(soapEnvelope, "downloadInvZipFkey");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DownloadInvZipFkey");
                throw;
            }
        }

        public async Task<string> DownloadInvZipToken(string invToken, string username, string password, bool checkPayment)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("downloadInvZipToken", new Dictionary<string, string>
                {
                    { "invToken", invToken },
                    { "userName", username },
                    { "userPass", password },
                    { "checkPayment", checkPayment.ToString().ToLower() }
                });

                return await SendSoapRequest(soapEnvelope, "downloadInvZipToken");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DownloadInvZipToken");
                throw;
            }
        }

        #endregion

        #region MTT (Máy tính tiền) APIs
        
        public async Task<string> ImportAndPublishInvMTT(string account, string acPass, string xmlInvData, 
            string username, string password, string pattern, string serial, int convert = 0)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("ImportAndPublishInvMTT", new Dictionary<string, string>
                {
                    { "Account", account },
                    { "ACpass", acPass },
                    { "xmlInvData", xmlInvData },
                    { "username", username },
                    { "password", password },
                    { "pattern", pattern },
                    { "serial", serial },
                    { "convert", convert.ToString() }
                });

                return await SendSoapRequest(soapEnvelope, "ImportAndPublishInvMTT");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ImportAndPublishInvMTT");
                throw;
            }
        }

        public async Task<string> ImportInvByPatternMTT(string account, string acPass, string xmlInvData, 
            string username, string password, string pattern, string serial, int convert = 0)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("ImportInvByPatternMTT", new Dictionary<string, string>
                {
                    { "Account", account },
                    { "ACpass", acPass },
                    { "xmlInvData", xmlInvData },
                    { "username", username },
                    { "password", password },
                    { "pattern", pattern },
                    { "serial", serial },
                    { "convert", convert.ToString() }
                });

                return await SendSoapRequest(soapEnvelope, "ImportInvByPatternMTT");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ImportInvByPatternMTT");
                throw;
            }
        }

        public async Task<string> SendInvMTT(string account, string acPass, string username, string password, 
            string pattern, string serial, string fkey)
        {
            try
            {
                var soapEnvelope = CreateSoapEnvelope("SendInvMTT", new Dictionary<string, string>
                {
                    { "Account", account },
                    { "ACpass", acPass },
                    { "username", username },
                    { "password", password },
                    { "pattern", pattern },
                    { "serial", serial },
                    { "fkey", fkey }
                });

                return await SendSoapRequest(soapEnvelope, "SendInvMTT");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendInvMTT");
                throw;
            }
        }

        #endregion

        #region Helper Methods

        private async Task<EInvoiceConfig> GetConfigAsync()
        {
            var config = await _context.EInvoiceConfigs.FirstOrDefaultAsync();
            if (config == null)
            {
                throw new InvalidOperationException("Chưa cấu hình thông tin hóa đơn điện tử. Vui lòng vào trang cấu hình để thiết lập.");
            }
            return config;
        }

        private async Task<string> SendSoapRequest(string soapEnvelope, string action)
        {
            var config = await GetConfigAsync();
            var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            content.Headers.Clear();
            content.Headers.Add("Content-Type", "text/xml; charset=utf-8");
            content.Headers.Add("SOAPAction", $"http://tempuri.org/{action}");

            var response = await _httpClient.PostAsync($"{config.ApiUrl}/PortalService.asmx", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation($"VNPT {action} Response: {responseContent}");

            return responseContent;
        }

        #endregion

        #region XML Generation

        private string CreateVNPTXmlInvoice(EInvoice invoice, EInvoiceConfig config)
        {
            try
            {
                var xmlDoc = new XDocument(
                    new XDeclaration("1.0", "UTF-8", null),
                    new XElement("Inv",
                        new XElement("key", Guid.NewGuid().ToString()),
                        new XElement("Invoice",
                            new XElement("CusCode", ""), // Mã khách hàng
                            new XElement("CusName", invoice.BuyerName ?? ""),
                            new XElement("CusAddress", invoice.BuyerAddress ?? ""),
                            new XElement("CusPhone", invoice.BuyerPhone ?? ""),
                            new XElement("CusTaxCode", invoice.BuyerTaxCode ?? ""),
                            new XElement("PaymentMethod", GetVNPTPaymentMethod(invoice.PaymentMethod)),
                            new XElement("KindOfService", ""), // Loại dịch vụ
                            new XElement("Products",
                                invoice.Items.Select((item, index) => 
                                    new XElement("Product",
                                        new XElement("ProdName", item.ItemName),
                                        new XElement("ProdUnit", item.Unit ?? ""),
                                        new XElement("ProdQuantity", item.Quantity.ToString(CultureInfo.InvariantCulture)),
                                        new XElement("ProdPrice", item.UnitPrice.ToString(CultureInfo.InvariantCulture)),
                                        new XElement("Amount", item.LineTotal.ToString(CultureInfo.InvariantCulture)),
                                        new XElement("VATRate", GetVNPTTaxRate(item.TaxRate)),
                                        new XElement("VATAmount", item.TaxAmount.ToString(CultureInfo.InvariantCulture)),
                                        new XElement("Total", item.TotalAmount.ToString(CultureInfo.InvariantCulture))
                                    )
                                ).ToArray()
                            ),
                            new XElement("Total", invoice.SubTotal.ToString(CultureInfo.InvariantCulture)),
                            new XElement("DiscountAmount", invoice.DiscountAmount.ToString(CultureInfo.InvariantCulture)),
                            new XElement("VATAmount", invoice.TaxAmount.ToString(CultureInfo.InvariantCulture)),
                            new XElement("Amount", invoice.TotalAmount.ToString(CultureInfo.InvariantCulture)),
                            new XElement("AmountInWords", NumberToVietnameseWords(invoice.TotalAmount)),
                            new XElement("ArisingDate", invoice.IssueDate.ToString("dd/MM/yyyy")),
                            new XElement("Note", invoice.Notes ?? "")
                        )
                    )
                );

                return xmlDoc.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating VNPT XML invoice");
                throw;
            }
        }

        private string CreateSOAPRequest(string methodName, Dictionary<string, object> parameters)
        {
            var soapBody = new StringBuilder();
            soapBody.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            soapBody.AppendLine("<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">");
            soapBody.AppendLine("  <soap:Body>");
            soapBody.AppendLine($"    <{methodName} xmlns=\"http://tempuri.org/\">");

            foreach (var param in parameters)
            {
                soapBody.AppendLine($"      <{param.Key}>{XmlEscape(param.Value?.ToString() ?? "")}</{param.Key}>");
            }

            soapBody.AppendLine($"    </{methodName}>");
            soapBody.AppendLine("  </soap:Body>");
            soapBody.AppendLine("</soap:Envelope>");

            return soapBody.ToString();
        }

        private string CreateSoapEnvelope(string methodName, Dictionary<string, string> parameters)
        {
            var soapBody = new StringBuilder();
            soapBody.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            soapBody.AppendLine("<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">");
            soapBody.AppendLine("  <soap:Body>");
            soapBody.AppendLine($"    <{methodName} xmlns=\"http://tempuri.org/\">");

            foreach (var param in parameters)
            {
                soapBody.AppendLine($"      <{param.Key}>{XmlEscape(param.Value ?? "")}</{param.Key}>");
            }

            soapBody.AppendLine($"    </{methodName}>");
            soapBody.AppendLine("  </soap:Body>");
            soapBody.AppendLine("</soap:Envelope>");

            return soapBody.ToString();
        }

        private EInvoiceApiResponse ParseVNPTResponse(string responseContent, string methodName)
        {
            try
            {
                var xmlDoc = XDocument.Parse(responseContent);
                var ns = XNamespace.Get("http://tempuri.org/");
                
                // Tìm response element
                var responseElement = xmlDoc.Descendants(ns + $"{methodName}Response").FirstOrDefault();
                var resultElement = responseElement?.Descendants(ns + $"{methodName}Result").FirstOrDefault();

                if (resultElement != null)
                {
                    var resultValue = resultElement.Value;
                    
                    // Parse kết quả từ VNPT (thường là XML hoặc string)
                    if (resultValue.Contains("OK") || resultValue.Contains("Success"))
                    {
                        // Extract transaction ID hoặc các thông tin cần thiết
                        var transactionId = ExtractTransactionId(resultValue);
                        
                        return new EInvoiceApiResponse
                        {
                            Success = true,
                            TransactionUuid = transactionId,
                            InvoiceAuthCode = ExtractAuthCode(resultValue),
                            ProcessedAt = DateTime.Now,
                            Data = resultValue
                        };
                    }
                    else
                    {
                        return new EInvoiceApiResponse
                        {
                            Success = false,
                            ErrorMessage = resultValue
                        };
                    }
                }

                return new EInvoiceApiResponse
                {
                    Success = false,
                    ErrorMessage = "Không thể parse response từ VNPT"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing VNPT response for {methodName}");
                return new EInvoiceApiResponse
                {
                    Success = false,
                    ErrorMessage = $"Lỗi parse response: {ex.Message}"
                };
            }
        }

        #endregion

        #region Helper Methods

        private string GetVNPTPaymentMethod(string? paymentMethod)
        {
            return paymentMethod?.ToLower() switch
            {
                "cash" => "TM", // Tiền mặt
                "card" => "CK", // Chuyển khoản
                "bank" => "CK",
                "qr" => "CK",
                _ => "TM"
            };
        }

        private string GetVNPTTaxRate(string taxRate)
        {
            return taxRate switch
            {
                "0%" => "0",
                "5%" => "5",
                "10%" => "10",
                "KCT" => "-1",
                "KKKNT" => "-2",
                _ => "10"
            };
        }

        private string XmlEscape(string text)
        {
            return text.Replace("&", "&amp;")
                      .Replace("<", "&lt;")
                      .Replace(">", "&gt;")
                      .Replace("\"", "&quot;")
                      .Replace("'", "&apos;");
        }

        private string ExtractTransactionId(string responseText)
        {
            // Logic để extract transaction ID từ response VNPT
            // Có thể cần điều chỉnh dựa trên format thực tế
            return Guid.NewGuid().ToString();
        }

        private string ExtractAuthCode(string responseText)
        {
            // Logic để extract auth code từ response VNPT
            return $"VNPT{DateTime.Now:yyyyMMddHHmmss}";
        }

        private string NumberToVietnameseWords(decimal amount)
        {
            // Chuyển đổi số thành chữ tiếng Việt
            // Đây là implementation đơn giản, có thể cải thiện
            var intPart = (long)Math.Floor(amount);
            return $"{intPart:N0} đồng".Replace(",", ".");
        }

        #endregion
    }
}