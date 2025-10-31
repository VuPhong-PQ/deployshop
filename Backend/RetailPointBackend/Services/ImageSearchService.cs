using System.Text.Json;
using System.Linq;
using System.Text.RegularExpressions;

namespace RetailPointBackend.Services
{
    public interface IImageSearchService
    {
        Task<string?> SearchAndDownloadImageAsync(string productName, string? productGroupName = null, string? description = null);
        Task<List<string>> SearchImagesAsync(string productName, int limit = 5, string? productGroupName = null, string? description = null);
    }

    public class ImageSearchService : IImageSearchService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ImageSearchService> _logger;
        private readonly string _unsplashAccessKey;
        private readonly string _uploadsPath;

        public ImageSearchService(HttpClient httpClient, ILogger<ImageSearchService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _unsplashAccessKey = configuration.GetValue<string>("UnsplashAccessKey") ?? "";
            _uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            
            // Tạo thư mục uploads nếu chưa tồn tại
            if (!Directory.Exists(_uploadsPath))
            {
                Directory.CreateDirectory(_uploadsPath);
            }
        }

        public async Task<string?> SearchAndDownloadImageAsync(string productName, string? productGroupName = null, string? description = null)
        {
            try
            {
                _logger.LogInformation($"Searching image for product: {productName}");
                
                // Tìm kiếm hình ảnh từ Unsplash
                var imageUrl = await SearchUnsplashImageAsync(productName, productGroupName, description);
                
                if (string.IsNullOrEmpty(imageUrl))
                {
                    _logger.LogWarning($"No image found for product: {productName}");
                    return null;
                }

                // Tải và lưu hình ảnh
                var localImagePath = await DownloadImageAsync(imageUrl, productName);
                return localImagePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching image for product: {productName}");
                return null;
            }
        }

        public async Task<List<string>> SearchImagesAsync(string productName, int limit = 5, string? productGroupName = null, string? description = null)
        {
            try
            {
                _logger.LogInformation($"Searching multiple images for product: {productName}");
                
                if (string.IsNullOrEmpty(_unsplashAccessKey) || _unsplashAccessKey == "YOUR_UNSPLASH_ACCESS_KEY_HERE")
                {
                    _logger.LogWarning("Unsplash access key not configured. Using fallback demo images.");
                    var fallbackImage = GetFallbackDemoImage(productName);
                    return fallbackImage != null ? new List<string> { fallbackImage } : new List<string>();
                }
                
                var searchTerm = CleanSearchTerm(productName, productGroupName, description);

                // Try several queries to increase chance of good matches (original, enhanced)
                var queries = new List<string> { productName, searchTerm };
                var candidates = new List<UnsplashPhoto>();

                foreach (var q in queries.Distinct())
                {
                    var url = $"https://api.unsplash.com/search/photos?query={Uri.EscapeDataString(q)}&per_page={limit}&orientation=squarish";
                    var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Add("Authorization", $"Client-ID {_unsplashAccessKey}");
                    var response = await _httpClient.SendAsync(request);
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning($"Unsplash API request failed for query '{q}': {response.StatusCode}");
                        continue;
                    }
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var searchResult = JsonSerializer.Deserialize<UnsplashSearchResponse>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (searchResult?.Results != null)
                        candidates.AddRange(searchResult.Results);
                }

                if (!candidates.Any()) return new List<string>();

                // Score candidates by textual similarity with searchTerm and productName
                var scored = candidates
                    .Select(p => new
                    {
                        Photo = p,
                        Score = ComputeTextScore(searchTerm, productName, (p.Description ?? "") + " " + (p.AltDescription ?? ""))
                    })
                    .OrderByDescending(x => x.Score)
                    .ThenByDescending(x => x.Photo.Urls?.Small != null ? 1 : 0)
                    .Select(x => x.Photo)
                    .DistinctBy(p => p.Urls?.Small ?? p.Urls?.Regular ?? Guid.NewGuid().ToString())
                    .ToList();

                return scored.Select(r => r.Urls?.Small ?? r.Urls?.Regular ?? string.Empty).Where(u => !string.IsNullOrEmpty(u)).Take(limit).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching multiple images for product: {productName}");
                return new List<string>();
            }
        }

        private async Task<string?> SearchUnsplashImageAsync(string productName, string? productGroupName = null, string? description = null)
        {
            if (string.IsNullOrEmpty(_unsplashAccessKey) || _unsplashAccessKey == "YOUR_UNSPLASH_ACCESS_KEY_HERE")
            {
                _logger.LogWarning("Unsplash access key not configured. Using fallback demo images.");
                return GetFallbackDemoImage(productName);
            }

            try
            {
                var searchTerm = CleanSearchTerm(productName, productGroupName, description);

                // Get multiple candidates and pick best by score
                var url = $"https://api.unsplash.com/search/photos?query={Uri.EscapeDataString(searchTerm)}&per_page=10&orientation=squarish";
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Client-ID {_unsplashAccessKey}");
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Unsplash API request failed: {response.StatusCode}");
                    return null;
                }
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var searchResult = JsonSerializer.Deserialize<UnsplashSearchResponse>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var candidates = searchResult?.Results ?? new List<UnsplashPhoto>();
                if (!candidates.Any()) return null;

                var scored = candidates
                    .Select(p => new { Photo = p, Score = ComputeTextScore(searchTerm, productName, (p.Description ?? "") + " " + (p.AltDescription ?? "")) })
                    .OrderByDescending(x => x.Score)
                    .ThenByDescending(x => x.Photo.Urls?.Small != null ? 1 : 0)
                    .FirstOrDefault();

                return scored?.Photo?.Urls?.Small ?? scored?.Photo?.Urls?.Regular;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calling Unsplash API for product: {productName}");
                return null;
            }
        }

        private static int ComputeTextScore(string enhancedTerm, string originalName, string description)
        {
            // Simple token overlap scoring
            if (string.IsNullOrEmpty(enhancedTerm) && string.IsNullOrEmpty(originalName)) return 0;

            var tokens = (enhancedTerm + " " + originalName).ToLower();
                tokens = Regex.Replace(tokens, @"[^a-z0-9\s]", " ");
            var tokenSet = new HashSet<string>(tokens.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries));

            var desc = (description ?? "").ToLower();
            desc = Regex.Replace(desc, @"[^a-z0-9\s]", " ");
            var descTokens = desc.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var score = descTokens.Count(t => tokenSet.Contains(t));

            // Prefer images with any description at all
            if (!string.IsNullOrEmpty(description)) score += 1;

            return score;
        }

        private async Task<string?> DownloadImageAsync(string imageUrl, string productName)
        {
            try
            {
                var response = await _httpClient.GetAsync(imageUrl);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to download image from: {imageUrl}");
                    return null;
                }
                
                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                var fileName = $"product_{Guid.NewGuid()}.jpg";
                var filePath = Path.Combine(_uploadsPath, fileName);
                
                await File.WriteAllBytesAsync(filePath, imageBytes);
                
                var relativeUrl = $"/uploads/{fileName}";
                _logger.LogInformation($"Image downloaded and saved: {relativeUrl}");
                
                return relativeUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading image from: {imageUrl}");
                return null;
            }
        }

        private string CleanSearchTerm(string productName, string? productGroupName = null, string? description = null)
        {
            // Làm sạch tên sản phẩm để tìm kiếm tốt hơn
            var cleanTerm = productName?.Trim().ToLower() ?? "";
            
            // Thêm thông tin từ nhóm sản phẩm và mô tả
            if (!string.IsNullOrEmpty(productGroupName))
            {
                cleanTerm += " " + productGroupName.Trim().ToLower();
            }
            
            if (!string.IsNullOrEmpty(description))
            {
                cleanTerm += " " + description.Trim().ToLower();
            }
            
            // Loại bỏ các ký tự đặc biệt
            cleanTerm = System.Text.RegularExpressions.Regex.Replace(cleanTerm, @"[^\w\s]", "");
            
            // Phân tích và cải thiện search term
            cleanTerm = EnhanceSearchTerm(cleanTerm);
            
            return cleanTerm;
        }

        private string EnhanceSearchTerm(string productName)
        {
            var enhancedTerm = productName.ToLower().Trim();
            
            // Dictionary để map từ Tiếng Việt sang Tiếng Anh và thêm context
            var productMapping = new Dictionary<string, string>
            {
                // Thực phẩm & Đồ uống
                {"xà phòng", "soap bar bathroom product"},
                {"xà bông", "soap bar bathroom product"},
                {"nước", "water bottle drink"},
                {"nước ngọt", "soft drink soda bottle"},
                {"coca", "coca cola drink bottle"},
                {"pepsi", "pepsi cola drink bottle"},
                {"bánh", "cake pastry food bakery"},
                {"bánh mì", "bread sandwich food bakery"},
                {"bánh quy", "cookie biscuit snack food"},
                {"kẹo", "candy sweet confectionery"},
                {"chocolate", "chocolate candy sweet"},
                {"sô cô la", "chocolate candy sweet"},
                {"sữa", "milk dairy bottle carton"},
                {"yogurt", "yogurt dairy cup container"},
                {"yaourt", "yogurt dairy cup container"},
                {"bia", "beer bottle alcohol drink"},
                {"rượu", "wine alcohol bottle drink"},
                {"cà phê", "coffee cup bean drink"},
                {"trà", "tea cup leaf drink"},
                {"gạo", "rice grain food package"},
                {"mì", "noodle instant food package"},
                {"mì tôm", "instant noodle ramen food"},
                {"thịt", "meat fresh food butcher"},
                {"cá", "fish fresh seafood"},
                {"tôm", "shrimp seafood fresh"},
                {"rau", "vegetable fresh produce"},
                {"củ", "root vegetable fresh produce"},
                {"quả", "fruit fresh produce"},
                {"táo", "apple fruit fresh red"},
                {"cam", "orange fruit fresh citrus"},
                {"chuối", "banana fruit fresh yellow"},
                {"xoài", "mango fruit fresh tropical"},
                
                // Điện tử & Công nghệ
                {"điện thoại", "smartphone mobile phone device"},
                {"smartphone", "smartphone mobile phone device"},
                {"iphone", "iphone smartphone apple device"},
                {"samsung", "samsung smartphone android device"},
                {"laptop", "laptop computer notebook device"},
                {"máy tính", "computer laptop desktop device"},
                {"máy tính bảng", "tablet ipad device touchscreen"},
                {"ipad", "ipad tablet apple device"},
                {"tai nghe", "headphones earphones audio device"},
                {"loa", "speaker audio bluetooth device"},
                {"tivi", "television tv screen electronics"},
                {"tủ lạnh", "refrigerator fridge appliance kitchen"},
                {"máy giặt", "washing machine appliance laundry"},
                {"máy lạnh", "air conditioner cooling appliance"},
                {"quạt", "fan cooling electric appliance"},
                
                // Thời trang & Phụ kiện
                {"áo", "shirt clothing fashion apparel"},
                {"áo thun", "t-shirt clothing casual wear"},
                {"áo sơ mi", "dress shirt formal clothing"},
                {"quần", "pants trousers clothing apparel"},
                {"quần jeans", "jeans denim pants clothing"},
                {"váy", "dress skirt women clothing"},
                {"giày", "shoes footwear fashion"},
                {"dép", "sandals slippers footwear"},
                {"túi", "bag handbag purse accessory"},
                {"ba lô", "backpack bag school travel"},
                {"mũ", "hat cap headwear fashion"},
                {"kính", "glasses eyewear accessory"},
                {"đồng hồ", "watch timepiece accessory jewelry"},
                {"nhẫn", "ring jewelry accessory"},
                {"dây chuyền", "necklace jewelry accessory"},
                
                // Mỹ phẩm & Chăm sóc cá nhân
                {"kem", "cream cosmetic skincare beauty"},
                {"son", "lipstick makeup cosmetic beauty"},
                {"phấn", "powder makeup cosmetic beauty"},
                {"nước hoa", "perfume fragrance beauty bottle"},
                {"dầu gội", "shampoo hair care bottle"},
                {"sữa tắm", "body wash shower gel bottle"},
                {"kem đánh răng", "toothpaste dental care tube"},
                {"bàn chải", "toothbrush dental care hygiene"},
                {"khăn", "towel bathroom textile"},
                
                // Đồ gia dụng
                {"nồi", "pot cookware kitchen utensil"},
                {"chảo", "pan frying cookware kitchen"},
                {"dao", "knife kitchen utensil cutlery"},
                {"thìa", "spoon cutlery kitchen utensil"},
                {"nĩa", "fork cutlery kitchen utensil"},
                {"bát", "bowl dish kitchen tableware"},
                {"đĩa", "plate dish kitchen tableware"},
                {"cốc", "cup glass drinking vessel"},
                {"ly", "glass cup drinking vessel"},
                
                // Văn phòng phẩm
                {"bút", "pen writing stationery office"},
                {"bút chì", "pencil writing stationery office"},
                {"thước", "ruler measuring stationery office"},
                {"vở", "notebook paper stationery office"},
                {"giấy", "paper sheet white stationery"},
                {"keo", "glue adhesive stationery office"},
                
                // Đồ chơi & Giải trí
                {"đồ chơi", "toy children play colorful"},
                {"búp bê", "doll toy children girl"},
                {"xe", "car toy vehicle children"},
                {"bóng", "ball sport toy round"},
                {"sách", "book reading literature paper"},
                {"truyện", "comic book manga reading"},
                
                // Thể thao
                {"giày thể thao", "sneakers athletic shoes sport"},
                {"áo thể thao", "sportswear athletic clothing"},
                {"bóng đá", "soccer ball football sport"},
                {"bóng rổ", "basketball ball orange sport"},
                {"vợt", "racket tennis badminton sport"}
            };
            
            // Tìm mapping chính xác
            if (productMapping.ContainsKey(enhancedTerm))
            {
                return productMapping[enhancedTerm];
            }
            
            // Tìm mapping gần đúng
            foreach (var mapping in productMapping)
            {
                if (enhancedTerm.Contains(mapping.Key) || mapping.Key.Contains(enhancedTerm))
                {
                    return mapping.Value;
                }
            }
            
            // Nếu không tìm thấy mapping, thêm từ khóa generic
            var categoryKeywords = DetectProductCategory(enhancedTerm);
            if (!string.IsNullOrEmpty(categoryKeywords))
            {
                enhancedTerm = $"{enhancedTerm} {categoryKeywords}";
            }
            
            // Thêm từ khóa "product" để tìm kiếm chính xác hơn
            enhancedTerm += " product commercial";
            
            return enhancedTerm;
        }

        private string DetectProductCategory(string productName)
        {
            // Phân loại sản phẩm dựa trên từ khóa
            var categories = new Dictionary<string[], string>
            {
                {new[] {"máy", "điện", "tử", "phone", "laptop", "computer"}, "electronics technology device"},
                {new[] {"áo", "quần", "giày", "mũ", "túi", "fashion", "clothing"}, "fashion clothing apparel wear"},
                {new[] {"thức", "ăn", "uống", "food", "drink", "nước", "bánh"}, "food drink consumable edible"},
                {new[] {"mỹ", "phẩm", "làm", "đẹp", "beauty", "cosmetic", "kem"}, "beauty cosmetic skincare makeup"},
                {new[] {"gia", "dụng", "nhà", "kitchen", "home", "nồi", "chảo"}, "home kitchen household appliance"},
                {new[] {"văn", "phòng", "office", "stationery", "bút", "giấy"}, "office stationery supplies"},
                {new[] {"thể", "thao", "sport", "fitness", "exercise", "bóng"}, "sports fitness athletic equipment"},
                {new[] {"đồ", "chơi", "toy", "children", "kid", "baby"}, "toy children play entertainment"},
                {new[] {"sách", "book", "reading", "education", "học"}, "book education reading literature"}
            };
            
            foreach (var category in categories)
            {
                if (category.Key.Any(keyword => productName.Contains(keyword)))
                {
                    return category.Value;
                }
            }
            
            return "retail product item";
        }

        private string? GetFallbackDemoImage(string productName)
        {
            // Danh sách các hình ảnh demo từ Unsplash (public domain)
            var demoImages = new Dictionary<string, string>
            {
                {"xà phòng", "https://images.unsplash.com/photo-1556228453-efd6c1ff04f6?w=400&h=400&fit=crop"},
                {"soap", "https://images.unsplash.com/photo-1556228453-efd6c1ff04f6?w=400&h=400&fit=crop"},
                {"nước", "https://images.unsplash.com/photo-1548839140-29a749e1cf4d?w=400&h=400&fit=crop"},
                {"water", "https://images.unsplash.com/photo-1548839140-29a749e1cf4d?w=400&h=400&fit=crop"},
                {"bánh", "https://images.unsplash.com/photo-1578985545062-69928b1d9587?w=400&h=400&fit=crop"},
                {"cake", "https://images.unsplash.com/photo-1578985545062-69928b1d9587?w=400&h=400&fit=crop"},
                {"kẹo", "https://images.unsplash.com/photo-1499195333224-3ce974eecb47?w=400&h=400&fit=crop"},
                {"candy", "https://images.unsplash.com/photo-1499195333224-3ce974eecb47?w=400&h=400&fit=crop"},
                {"nước ngọt", "https://images.unsplash.com/photo-1437418747212-8d9709afab22?w=400&h=400&fit=crop"},
                {"soda", "https://images.unsplash.com/photo-1437418747212-8d9709afab22?w=400&h=400&fit=crop"},
                {"điện thoại", "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=400&h=400&fit=crop"},
                {"phone", "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=400&h=400&fit=crop"},
                {"laptop", "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=400&h=400&fit=crop"},
                {"máy tính", "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=400&h=400&fit=crop"}
            };

            var searchKey = productName.ToLower().Trim();
            
            // Tìm chính xác
            if (demoImages.ContainsKey(searchKey))
            {
                _logger.LogInformation($"Using demo image for: {productName}");
                return demoImages[searchKey];
            }
            
            // Tìm gần đúng
            foreach (var key in demoImages.Keys)
            {
                if (searchKey.Contains(key) || key.Contains(searchKey))
                {
                    _logger.LogInformation($"Using similar demo image for: {productName}");
                    return demoImages[key];
                }
            }
            
            // Hình ảnh mặc định
            _logger.LogInformation($"Using default demo image for: {productName}");
            return "https://images.unsplash.com/photo-1560472354-b33ff0c44a43?w=400&h=400&fit=crop";
        }
    }

    // Models for Unsplash API
    public class UnsplashSearchResponse
    {
        public List<UnsplashPhoto>? Results { get; set; }
    }

    public class UnsplashPhoto
    {
        public UnsplashUrls? Urls { get; set; }
        public string? Description { get; set; }
        public string? AltDescription { get; set; }
    }

    public class UnsplashUrls
    {
        public string? Raw { get; set; }
        public string? Full { get; set; }
        public string? Regular { get; set; }
        public string? Small { get; set; }
        public string? Thumb { get; set; }
    }
}