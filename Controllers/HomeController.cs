using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using N7.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using N7.ViewModels;
using System.IO;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using System.Web;
using System.Web.UI;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Extensions;
using System.Data;


namespace N7.Controllers
{
    public class AppCommentWihName : AppComment
    {
        public string AuthorName { get; set; }
    }
    
    public static class RequestExtensions
    {
        //regex from http://detectmobilebrowsers.com/
        private static readonly Regex b = new Regex(@"(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows ce|xda|xiino", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        private static readonly Regex v = new Regex(@"1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        public static bool IsMobileBrowser(this HttpRequest request)
        {
            var userAgent = request.UserAgent();
            if ((b.IsMatch(userAgent) || v.IsMatch(userAgent.Substring(0, 4))))
            {
                return true;
            }

            return false;
        }

        public static string UserAgent(this HttpRequest request)
        {
            return request.Headers["User-Agent"];
        }
    }

    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IStringLocalizer<HomeController> _localizer;
        private readonly IStringLocalizer<SharedResource> _sharedLocalizer;
        private ApplicationContext database;
        private static SearchClient _searchClient;
        private static SearchIndexClient _indexClient;
        private static IConfigurationBuilder _builder;
        private static IConfigurationRoot _configuration;
        
        public HomeController(ILogger<HomeController> logger, UserManager<AppUser> userManager,
                                  SignInManager<AppUser> signInManager, RoleManager<IdentityRole> roleManager,
                                IStringLocalizer<HomeController> localizer,
                                IStringLocalizer<SharedResource> sharedLocalizer,
                                ApplicationContext context)
        {
            _roleManager = roleManager;
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _localizer = localizer;
            _sharedLocalizer = sharedLocalizer;
            database = context;
        }

        public bool IsMobile(HttpRequest request)
        {
            try
            {
                string mobileDesktop = Request.Query["mobileDesktop"];                
                if (mobileDesktop == "Mobile")
                    return true;
                if (mobileDesktop == "Desktop")
                    return false;
                string mobileDesktopCookie = Request.Cookies["mobileDesktop"];
                if (mobileDesktopCookie == "Mobile")
                    return true;
                if (mobileDesktopCookie == "Desktop")
                    return false;
            }
            catch { }
            if (RequestExtensions.IsMobileBrowser(request))
                return true;            
            return false;
        }
        
        public ActionResult AutoComplete(string term)
        {
            int x = term.LastIndexOf(" ");
            string term2 = (x>0)&&(x<term.Length-1) ? 
                term.Substring(x+1):term;                        
            var model = TagsCloud()
                    .Where(p => (p.StartsWith(term2, StringComparison.InvariantCultureIgnoreCase)))
                    .Take(5)
                    .Select(p => new
                    {                
                        label = p.Trim().ToLower()
                    });                
                return Json(model);                                                    
        }

        public bool MapTag (string a="", string b="")
        {                           
            if ((b == "")||(b==null)) return true;
            else
            if ((a==null)||(a=="")) return false;
            var alist = TagsToList(a);
            var blist = TagsToList(b);            
            foreach (var x in blist)
                if (!alist.Exists(p => p.ToLower() == x.ToLower()))
                    return false;
            return true;
        }
        
        public List<string> TagsToList(string a)
        {
            return (a != null)&&(a!="") ?
                    Regex.Matches(a, @"[\""].+?[\""]|[^ ]+")
                    .Cast<Match>()
                    .Select(a => a.Value)
                    .ToList()
                    : null;
        }

        public string TagListToString(List<AppTag> a)
        {
            string r="";
            foreach(var x in a)
            {
                r += x.Text + " ";
            }
            
            return r.Trim();
        }

        public string TagsItemLinks(List<string> x)
        {
            string r = "";
            foreach(var a in x)
            {
                r += "<a href=\"/home/FindItems?SearchByTag=";
                foreach (var c in a)
                {
                    r += c == '\"' ? "%22" : c;                     
                }
                r+= "\">" + a + "</a> ";
            }
            return r;
        }
        
        public HashSet<string> TagsCloud()
        {
            var r = new HashSet<string>();            
            foreach (var x in database.AppTags)
            {
                if ((x.Text != null) && (x.Text != ""))
                {
                    r.Add(x.Text);                                          
                }
            }
            return r;
        }

        public void TagsCloudExec()
        {            
            database.AppTagCloudRecords.ToList()
                .ForEach(p=> database.AppTagCloudRecords.Remove(p));
            database.SaveChanges();
            List<AppTagCloudRecord> L = new List<AppTagCloudRecord>();
            foreach(var x in database.AppTags.ToList())
            {               
                AppTagCloudRecord r = 
                    L.ToList().Find(p => p.Text.ToLower() == x.Text.ToLower());
                if (r == null)                     
                {
                    r = new AppTagCloudRecord();
                    r.Text = x.Text.ToLower();
                    r.Power = 1;
                    L.Add(r);
                }
                else
                {                                     
                    L[L.IndexOf(r)].Power++;                   
                }
            }
            L.ForEach(p => database.AppTagCloudRecords.Add(p));
            database.SaveChanges();
        }
        
        private SelectList GetAppThemes()
        {
            SelectList AppThemes =
                new SelectList(database.AppThemes, "Id", "Name", 0);
            return AppThemes;
        }
        private SelectList GetDataTypes(string Selected="0")
        {
            List<AppTheme> x = new List<AppTheme>();
            x.Add(new AppTheme { Id = "0", Name = "Bool" });
            x.Add(new AppTheme { Id = "1", Name = "Date" });
            x.Add(new AppTheme { Id = "2", Name = "Number" });
            x.Add(new AppTheme { Id = "3", Name = "String" });
            x.Add(new AppTheme { Id = "4", Name = "Text" });                                                    
            SelectList AppDataTypes =
                new SelectList(x, "Id", "Name", Selected);
            return AppDataTypes;
        }
        private void InitSearch()
        {            
            _builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
            _configuration = _builder.Build();
            string searchServiceUri = _configuration["SearchServiceUri"];
            string queryApiKey = _configuration["SearchServiceQueryApiKey"];
            _indexClient = new SearchIndexClient(new Uri(searchServiceUri), new AzureKeyCredential(queryApiKey));
            _searchClient = _indexClient.GetSearchClient("n7index");
        }

        private async Task<ActionResult> RunQueryAsync(SearchData model)
        {
            InitSearch();
            var options = new SearchOptions()
            {
                IncludeTotalCount = true
            };
            options.Select.Add("Id");
            options.Select.Add("Name");            
            model.ResultList = await _searchClient.SearchAsync<AppItem>(model.SearchText, options).ConfigureAwait(false);
            return View("Search", model);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AppThemeList()
        {
            ViewBag.AppThemes = database.AppThemes.ToList();
            AppTheme a = new AppTheme();
            return View(a);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAppTheme(AppTheme apptheme)
        {
            if (apptheme != null)
            {
                database.AppThemes.Add(apptheme);
                await database.SaveChangesAsync();
            }
            return RedirectToAction("AppThemeList");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAppTheme(AppTheme apptheme)
        {
            if (apptheme != null)
            {
                database.AppThemes.Remove(apptheme);
                await database.SaveChangesAsync();
            }
            return RedirectToAction("AppThemeList");
        }

        public IActionResult XFindCollections(AppCollectionSearch r)
        {
            return RedirectToAction("FindCollections", new AppCollectionSearch
            { UserName = r.UserName, Details = r.Details, Name = r.Name,
                Id = r.Id, AppThemeId = r.AppThemeId, AppThemeName = r.AppThemeName });
        }

        public IActionResult FindCollections(string authorId, AppCollectionSearch model)
        {
            ViewBag.IsMobile = IsMobile(Request);
            ViewBag.Themes = GetAppThemes();
            AppCollectionSearch c = new AppCollectionSearch();
            string ThemeName = "<Choose>";
            if (authorId != null)
            {
                AppUser user = _userManager.Users.ToList().Find(x => x.Id == authorId);
                c.UserName = user.UserName;
            }

            if (model.AppThemeId != null)
            {
                AppTheme a = new AppTheme();
                a = database.AppThemes.ToList().Find(x => x.Id == model.AppThemeId);
                ThemeName = a.Name;
            }

            var query = database.AppCollections
                .Include(x => x.AppUser).Include(x => x.AppTheme)
                        .Select(x=> new
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Details = x.Details,
                            AppThemeId = x.AppThemeId,
                            ThemeName = x.AppTheme.Name,
                            AppUserId = x.AppUserId,
                            UserName = x.AppUser.UserName
                        });
            
            var collections = (authorId == null) ? query
                .Where(p => ((p.UserName.Contains(model.UserName))
                            || (model.UserName == null))
                        && (p.Name.Contains(model.Name)
                            || (model.Name == null))
                        && (p.Details.Contains(model.Details)
                            || (model.Details == null))
                         && ((p.AppThemeId == model.AppThemeId)
                            || (ThemeName == "<Choose>"))
                            ).ToList() : query
                .Where(p => p.AppUserId == authorId).ToList();
           
            List<AppCollectionSearch> L = new List<AppCollectionSearch>();
            
            if ((authorId != null) || (model.AppThemeId != null))
            {
                foreach (var x in collections)
                {
                    
                    L.Add(new AppCollectionSearch
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Details = x.Details,
                        AppUserId = x.AppUserId,
                        UserName = x.UserName,
                        AppThemeId = x.AppThemeId,
                        AppThemeName = x.ThemeName
                    }); 
                }
            }
            
            ViewBag.Collections = L;            
            return View(c);
        }

        public IActionResult XFindItems(AppItemSearch r)
        {
            return RedirectToAction("FindItems", new AppItemSearch
            {
                UserName = r.UserName,
                Tags = r.Tags,
                Name = r.Name,
                Id = r.Id,
                AppThemeId = r.AppThemeId,
                ThemeName = r.ThemeName,
                AppCollectionId = r.AppCollectionId,
                CollectionName = r.CollectionName
            });
        }
        public IActionResult FindItems(AppItemSearch itemSearch, string SearchByTag="")
        {
            ViewBag.IsMobile = IsMobile(Request);
            AppItemSearch model = new AppItemSearch();
            model = itemSearch;
            if (itemSearch.AppThemeId != null)
            {
                AppTheme a = new AppTheme();
                a = database.AppThemes.ToList().Find(x => x.Id == itemSearch.AppThemeId);
                model.ThemeName = a.Name;
            }

            ViewBag.Themes = GetAppThemes();

            var query = from AppItem in database.AppItems
                        join AppCollection in database.AppCollections
                        on AppItem.AppCollectionId equals AppCollection.Id
                        join AppUser in database.Users
                        on AppCollection.AppUserId equals AppUser.Id
                        join AppTheme in database.AppThemes
                        on AppCollection.AppThemeId equals AppTheme.Id
                        select new { Id = AppItem.Id,
                            Name = AppItem.Name,
                            CollectionId = AppCollection.Id,
                            CollectionName = AppCollection.Name,
                            Tags = "", 
                            ThemeId = AppTheme.Id,
                            ThemeName = AppTheme.Name,
                            UserId = AppUser.Id,
                            UserName = AppUser.UserName };

            var resultItems = SearchByTag=="" ? query
                 .Where(p => (p.UserName.Contains(model.UserName)
                           || (model.UserName == null))
                           && (p.Name.Contains(model.Name)
                           || (model.Name == null))                                                      
                           && (p.CollectionName.Contains(model.CollectionName)
                           || (model.CollectionName == null))
                           && ((p.ThemeId == model.AppThemeId)
                           || (model.ThemeName == "<Choose>"))
                            ):query;
                                                                                  
            List<AppItemSearch> L = new List<AppItemSearch>();
            var itemTags = database.AppTags
                .Where(x => x.Text.ToLower() == SearchByTag.ToLower()).ToList();

            foreach (var x in resultItems.ToList())
             {
                var xTags = TagListToString(database.AppTags
                            .Where(p => p.AppItemId == x.Id).ToList());
                
                if ((SearchByTag == "")
                   && (MapTag(xTags,model.Tags))
                   || (itemTags.Exists(p => p.AppItemId == x.Id)))

                {                                       
                    L.Add(new AppItemSearch
                    {
                         Id = x.Id,
                         Name = x.Name,
                         Tags = (xTags!="")&&(xTags!=null)?
                              TagsItemLinks(TagsToList(xTags)) : "",
                         AppCollectionId = x.CollectionId,
                         CollectionName = x.CollectionName,
                         AppUserId = x.UserId,
                         UserName = x.UserName,
                         AppThemeId = x.ThemeId,
                         ThemeName = x.ThemeName
                    });                    
                }
             }
            
            ViewBag.ItemList = L;
            return View(model);
        }

        public IActionResult Authors()
        {
            return View(_userManager.Users.ToList());
        }
        public IActionResult Create()
        {
            ViewBag.Themes = GetAppThemes();
            return View();
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(AppCollection appCollection, 
            AppHeader[] NewAppHeaders, string returnUrl)
        {
            AppUser user = new AppUser();
            
            if ((User.IsInRole("Admin")) && (appCollection.AppUserId != null)
                && (appCollection.AppUserId != ""))
            {
                user.Id = appCollection.AppUserId;
            }
            else
            {
                user = await _userManager.FindByNameAsync(User.Identity.Name);
            }
            foreach (var x in NewAppHeaders)
            {
                if ((x.Name != null) && (x.Name != ""))
                {
                    x.AppCollection = appCollection;
                    x.AppCollectionId = appCollection.Id;
                    appCollection.AppHeaders.Add(x);
                }
            }
            appCollection.AppUserId = user.Id;
            database.AppCollections.Add(appCollection);
            await database.SaveChangesAsync();
            return Redirect(returnUrl);
        }

        public async Task<IActionResult> DetailsItem(string id)
        {
            if (id != null)
            {
                AppItem item = await database.AppItems.FindAsync(id);
                AppCollection appcollection =
                    await database.AppCollections.FindAsync(item.AppCollectionId);
                ViewData["CollectionName"] = appcollection.Name;
                AppUser user = await _userManager.FindByIdAsync(appcollection.AppUserId);
                ViewData["CollectionAuthorName"] = user.UserName;
                ViewData["CollectionAuthorId"] = user.Id;
                ViewBag.Item = item;
                ViewBag.UserId = user.Id;
                ViewBag.AuthorHere = false;
                if ((User.IsInRole("Admin")||(user.UserName==User.Identity.Name)))
                {
                    ViewBag.AuthorHere = true;
                }
                                    
                ViewBag.Collection = appcollection;
                ViewBag.CreateDateString = item.CreateDate.ToShortDateString();

                item.AppCollection = appcollection;
                item.AppCollection.AppHeaders =
                    database.AppHeaders.Where(x => x.AppCollectionId == appcollection.Id)
                    .OrderBy(x => x.DataType).ThenBy(x=>x.Name).ToList();
                item.AppBools =
                    database.AppBools.Where(x => x.AppItemId == item.Id).ToList();
                item.AppDates =
                    database.AppDates.Where(x => x.AppItemId == item.Id).ToList();
                item.AppNumbers =
                    database.AppNumbers.Where(x => x.AppItemId == item.Id).ToList();
                item.AppStrings =
                    database.AppStrings.Where(x => x.AppItemId == item.Id).ToList();
                item.AppTexts =
                    database.AppTexts.Where(x => x.AppItemId == item.Id).ToList();

                IQueryable<AppComment> comments = database.AppComments
                    .Where(p => p.AppItemId == id).OrderBy(p => p.Time);
                ViewBag.Comments = comments.ToList();

                IQueryable<AppLike> likes = database.AppLikes.Where(p => p.AppItemId == id);
                ViewBag.Likes = likes.ToList().Count;

                AppUser currentUser = new AppUser();
                AppComment c = new AppComment();
                bool liked = false;
                if (User.Identity.IsAuthenticated)
                {
                    currentUser = await _userManager.FindByNameAsync(User.Identity.Name);
                    liked = likes.ToList().Exists(p => p.AppUserId == currentUser.Id);
                    
                    c.AppUserId = currentUser.Id;
                    c.AppItemId = item.Id;
                }

                ViewBag.Liked = liked;

                foreach (var x in comments.ToList())
                {
                    AppUser user2 = await _userManager.FindByIdAsync(x.AppUserId);
                    ViewData[x.Id] = user2.UserName;
                }

                ViewBag.TagList = database.AppTags
                    .Where(x => x.AppItemId == item.Id).ToList();
                
                if (appcollection != null)
                    return View(c);
            }
            return NotFound();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddComment(AppComment appComment)
        {
            AppUser user = await _userManager.FindByNameAsync(User.Identity.Name);
            
            if ((appComment.AppItemId != null) && ((appComment.AppUserId == user.Id)
                    || (User.IsInRole("Admin"))))
            {                
                appComment.Time = DateTime.Now;
                database.AppComments.Add(appComment);
                await database.SaveChangesAsync();
            }
            return RedirectToAction("DetailsItem", new { id = appComment.AppItemId });
        }

        public string FreshComments(string id)
        {
            List<AppCommentWihName> CommentsWithNames = new List<AppCommentWihName>();

            IQueryable<AppComment> comments =
                database.AppComments.Where(p => p.AppItemId == id);

            foreach (var x in comments.ToList())
            {
                AppCommentWihName a = new AppCommentWihName();
                a.AppUserId = x.AppUserId;
                a.Text = x.Text;
                a.Id = x.Id;
                AppUser user = _userManager.Users.ToList().Find(p => p.Id == a.AppUserId);
                a.AuthorName = user.UserName;
                a.AppItemId = x.AppItemId;
                CommentsWithNames.Add(a);
            }

            string json = JsonSerializer.Serialize<List<AppCommentWihName>>(CommentsWithNames);
            json = "{ \"comments\": " + json + "}";

            return json;
        }

        [Authorize]
        public string AddLike(string ItemId, string AuthorId)
        {
            IQueryable<AppLike> likes =
                database.AppLikes.Where(x => (x.AppUserId == AuthorId)
                                          && (x.AppItemId == ItemId));

            if (likes.ToList().Count == 0)
            {
                AppLike a = new AppLike();
                a.AppUserId = AuthorId;
                a.AppItemId = ItemId;
                a.Time = DateTime.Now;
                database.AppLikes.Add(a);
                database.SaveChanges();
            }

            likes = database.AppLikes.Where(p => p.AppItemId == ItemId);
            return Convert.ToString(likes.ToList().Count);

        }

        [Authorize]
        public string DeleteLike(string ItemId, string AuthorId)
        {
            IQueryable<AppLike> likes =
                database.AppLikes.Where(x => (x.AppUserId == AuthorId)
                                          && (x.AppItemId == ItemId));
            if (likes.ToList().Count > 0)
            {
                database.AppLikes.Remove(likes.ToList()[0]);
                database.SaveChanges();
            }
            likes = database.AppLikes.Where(p => p.AppItemId == ItemId);
            return Convert.ToString(likes.ToList().Count);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteComment(string commentId, string itemNum)
        {
            AppComment comment = await database.AppComments.FindAsync(commentId);
            if (comment != null)
            {
                database.AppComments.Remove(comment);
                database.SaveChanges();
            }
            return RedirectToAction("DetailsItem", new { id = itemNum });
        }

        [Authorize]
        public async Task<IActionResult> Edit(string id="", string returnUrl="", bool create=false)
        {
            if (id != null)
            {
                ViewBag.Themes = GetAppThemes();
                ViewBag.DataTypes = GetDataTypes();
                ViewBag.ReturnUrl = returnUrl;

                AppCollection appcollection = new AppCollection();
                if (!create)
                  appcollection = await database.AppCollections.FindAsync(id);

                ViewBag.Collection = appcollection;
                if ((!create)&&(appcollection != null))
                {
                    ViewBag.Action = "edit";
                  
                    var headers = database.AppHeaders
                        .Where(x => x.AppCollectionId == appcollection.Id).ToList();
                    var headersTypes = new List<SelectList>();
                    foreach(var x in headers)
                    {
                        var a = GetDataTypes(x.DataType);
                        headersTypes.Add(a);
                    }
                    ViewBag.Types = headersTypes;
                    ViewBag.AppHeaders = headers;
                    return View();
                }
                if (create)
                {                    
                    ViewBag.Action = "create";
                    return View();
                }
            }
            return NotFound();
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Edit( AppCollection appcollection,
            AppHeader[] NewAppHeaders, string returnUrl)
        {
            AppUser user = await _userManager.FindByNameAsync(User.Identity.Name);
            if ((appcollection.Id != null) && ((appcollection.AppUserId == user.Id)
                    || User.IsInRole("Admin")))
            {             
                for(int i=0;i<appcollection.AppHeaders.Count();i++)
                {                    
                    if ((appcollection.AppHeaders[i].Name == "") ||
                        (appcollection.AppHeaders[i].Name == null))
                    {                                    
                        database.AppHeaders.Remove(appcollection.AppHeaders[i]);
                        appcollection.AppHeaders.Remove(appcollection.AppHeaders[i]);                  
                        i--;
                    }
                }

                foreach (var x in NewAppHeaders)
                {
                    if ((x.Name != null) && (x.Name != ""))
                    {
                        x.AppCollection = appcollection;
                        x.AppCollectionId = appcollection.Id;
                        appcollection.AppHeaders.Add(x);
                    }
                }
                
                database.AppCollections.Update(appcollection);
                await database.SaveChangesAsync();

                foreach (var x in database.AppHeaders
                    .Where(p => p.AppCollectionId == appcollection.Id).ToList())
                {
                    database.AppItems.Where(a => a.AppCollectionId == appcollection.Id)
                        .ToList().ForEach(c =>
                        {
                            switch (x.DataType)
                            {
                                case "0":
                                    if (!(database.AppBools.ToList()
                                    .Exists(a => (a.AppHeaderId == x.Id)
                                              && (a.AppItemId == c.Id))))
                                    {
                                        AppBool r0 = new AppBool();
                                        r0.AppItemId = c.Id;
                                        r0.AppHeaderId = x.Id;
                                        database.AppBools.Add(r0);
                                    }
                                    break;
                                case "1":
                                    if (!(database.AppDates.ToList()
                                    .Exists(a => (a.AppHeaderId == x.Id)
                                              && (a.AppItemId == c.Id))))
                                    {
                                        AppDate r1 = new AppDate();
                                        r1.AppItemId = c.Id;
                                        r1.AppHeaderId = x.Id;
                                        database.AppDates.Add(r1);
                                    }
                                    break;
                                case "2":
                                    if (!(database.AppNumbers.ToList()
                                    .Exists(a => (a.AppHeaderId == x.Id)
                                              && (a.AppItemId == c.Id))))
                                    {
                                        AppNumber r2 = new AppNumber();
                                        r2.AppItemId = c.Id;
                                        r2.AppHeaderId = x.Id;
                                        database.AppNumbers.Add(r2);
                                    }
                                    break;
                                case "3":
                                    if (!(database.AppStrings.ToList()
                                    .Exists(a => (a.AppHeaderId == x.Id)
                                              && (a.AppItemId == c.Id))))
                                    {
                                        AppString r3 = new AppString();
                                        r3.AppItemId = c.Id;
                                        r3.AppHeaderId = x.Id;
                                        database.AppStrings.Add(r3);
                                    }
                                    break;
                                case "4":
                                    if (!(database.AppTexts.ToList()
                                    .Exists(a => (a.AppHeaderId == x.Id)
                                              && (a.AppItemId == c.Id))))
                                    {
                                        AppText r4 = new AppText();
                                        r4.AppItemId = c.Id;
                                        r4.AppHeaderId = x.Id;
                                        r4.Value = "";
                                        database.AppTexts.Add(r4);
                                    }
                                    break;
                            }
                        });
                }
                
                database.AppCollections.Update(appcollection);
                await database.SaveChangesAsync();
            }
            return Redirect(returnUrl);
        }

        public async Task<IActionResult> EditCollectionItems(string id)
        {
            ViewBag.AuthorHere = false;
            ViewBag.IsMobile = IsMobile(Request);
            AppCollection model = await database.AppCollections.FindAsync(id);

            AppUser user = new AppUser();
            if (User.Identity.IsAuthenticated)
            {
                user = await _userManager.FindByNameAsync(User.Identity.Name);
            }

            AppUser author = await _userManager.FindByIdAsync(model.AppUserId);

            AppTheme apptheme = await database.AppThemes.FindAsync(model.AppThemeId);
            if (user.Id == model.AppUserId)
            {
                ViewBag.AuthorHere = true;
            }
            ViewBag.Collection = model;
            ViewBag.Theme = apptheme;
            ViewBag.Author = author;

            var items = database.AppItems.Where(p => p.AppCollectionId == id).ToList();
            var appHeaders = database.AppHeaders.Where(p => p.AppCollectionId == id)
                              .OrderBy(x => x.Name).ToList();

            ViewBag.AppHeaders = appHeaders;

            ViewBag.c = appHeaders.Count;

            DataTable table = new DataTable("ParentTable");
            DataColumn column;
            DataRow row;            
            column = new DataColumn();
            column.DataType = System.Type.GetType("System.String");
            column.ColumnName = "Id";
            column.ReadOnly = true;
            column.Unique = true;            
            table.Columns.Add(column);
           
            column = new DataColumn();
            column.DataType = System.Type.GetType("System.String");
            column.ColumnName = "Name";
            column.AutoIncrement = false;
            column.Caption = "Name";
            column.ReadOnly = false;
            column.Unique = false;
            table.Columns.Add(column);

            column = new DataColumn();
            column.DataType = System.Type.GetType("System.String");
            column.ColumnName = "Tags";
            column.AutoIncrement = false;
            column.Caption = "Tags";
            column.ReadOnly = false;
            column.Unique = false;
            table.Columns.Add(column);
                         
            int counter = 0;
            foreach (var x in appHeaders)
            {                
                string a = "";
                switch (x.DataType)
                {
                    case "0": a = "System.Boolean"; break;
                    case "1": a = "System.DateTime"; break;
                    case "2": a = "System.Double"; break;
                    case "3": a = "System.String"; break;
                    case "4": continue; 

                }
                column = new DataColumn();
                column.DataType = System.Type.GetType(a);
                column.ColumnName = x.Id;
                column.AutoIncrement = false;
                column.Caption = x.Id;
                column.ReadOnly = false;
                column.Unique = false;                
                table.Columns.Add(column);
                counter++;
            }
            DataColumn[] PrimaryKeyColumns = new DataColumn[1];
            PrimaryKeyColumns[0] = table.Columns["Id"];
            table.PrimaryKey = PrimaryKeyColumns;
            for (int i = 0; i < items.Count; i++)
            {
                row = table.NewRow();
                row["Id"] = items[i].Id;
                row["Name"] = items[i].Name;

                var itemTags = database.AppTags
                    .Where(p => p.AppItemId == items[i].Id).ToList();

                row["Tags"] = itemTags.Count>0 ?
                    TagsItemLinks(TagsToList(TagListToString(itemTags))):"";
                foreach(var x in appHeaders)
                {
                    switch (x.DataType)
                    {
                        case "0":
                            var r0 = database.AppBools.ToList()
                                .Find(a => (a.AppHeaderId == x.Id)
                                     && (a.AppItemId == items[i].Id));
                            row[x.Id] = r0.Value;
                            break;
                        case "1":
                            var r1 = database.AppDates.ToList()
                                .Find(a => (a.AppHeaderId == x.Id)
                                     && (a.AppItemId == items[i].Id));
                            row[x.Id] = r1.Value;
                            break;
                        case "2":                       
                            var r2 = database.AppNumbers.ToList()
                                .Find(a => (a.AppHeaderId == x.Id)
                                     && (a.AppItemId == items[i].Id));
                            row[x.Id] = r2.Value;
                            break;
                        case "3":
                            var r3 = database.AppStrings.ToList()
                                .Find(a => (a.AppHeaderId == x.Id)
                                     && (a.AppItemId == items[i].Id));
                            row[x.Id] = r3.Value;
                            break;
                        case "4": continue;
                    }
                    
                }                
                table.Rows.Add(row);
            }

            ViewBag.c2 = counter;            
            ViewBag.Data = table;                                                   
            List < AppItemSearch > L = new List<AppItemSearch>();
            foreach (var x in items)
            {
                 L.Add(new AppItemSearch
                 {
                    Id = x.Id,
                    Name=x.Name,
                    CreateDate = x.CreateDate
                    });
                }                               
            if (model != null)
                return View(model);            
            return NotFound();
        }
        
        [Authorize]
        public async Task<IActionResult> CreateItem(string collectionId, string returnUrl)
        {
            AppItem model = new AppItem();
            model.AppCollectionId = collectionId;
            AppCollection a = await database.AppCollections
                .FindAsync(collectionId);
            ViewBag.Collection = a;
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateItem(AppItem appItem, string returnUrl2)
        {
            AppItem a = appItem;            
            a.CreateDate = DateTime.Parse("01/01/1703");
            AppUser user = await _userManager.FindByNameAsync(User.Identity.Name);
            AppCollection appcollection = await
                database.AppCollections.FindAsync(a.AppCollectionId);
            var headers = database.AppHeaders.Where(x => x.AppCollectionId == appcollection.Id).ToList();

            if ((appcollection.Id != null) && ((appcollection.AppUserId == user.Id)
                    || User.IsInRole("Admin")))
            {
                database.AppItems.Add(a);
                foreach(var x in headers)
                {
                    switch(x.DataType)
                    {
                        case "0":
                            AppBool r0 = new AppBool();
                            r0.AppItemId = a.Id;
                            r0.AppHeaderId = x.Id;
                            database.AppBools.Add(r0);
                            break;
                        case "1":
                            AppDate r1 = new AppDate();
                            r1.AppItemId = a.Id;
                            r1.AppHeaderId = x.Id;
                            database.AppDates.Add(r1);
                            break;
                        case "2":
                            AppNumber r2 = new AppNumber();
                            r2.AppItemId = a.Id;
                            r2.AppHeaderId = x.Id;
                            database.AppNumbers.Add(r2);
                            break;
                        case "3": 
                            AppString r3 = new AppString();
                            r3.AppItemId = a.Id;
                            r3.AppHeaderId = x.Id;
                            database.AppStrings.Add(r3);
                            break;
                        case "4":
                            AppText r4 = new AppText();
                            r4.AppItemId = a.Id;
                            r4.AppHeaderId = x.Id;
                            database.AppTexts.Add(r4);
                            break;
                    }

                }
                                                
                await database.SaveChangesAsync();
                AppItem NewItem = database.AppItems
                    .Where(x => x.AppCollectionId == a.AppCollectionId)
                    .First(x => x.CreateDate.Year == 1703);
                NewItem.CreateDate = DateTime.Now;
                database.AppItems.Update(NewItem);
                await database.SaveChangesAsync();
                return RedirectToAction("EditItem",
                    new { id = NewItem.Id, returnUrl = returnUrl2 });                
            }
            return RedirectToAction("EditCollectionsItem", new { id = a.AppCollectionId });
        }
                        
        [Authorize]        
        public async Task<IActionResult> EditItem(string id, string returnUrl)
        {           
            if (id != null)
            {
                AppItem item = await database.AppItems.FindAsync(id);
                AppCollection appcollection =
                    await database.AppCollections.FindAsync(item.AppCollectionId);
                item.AppCollection = appcollection;                
                item.AppCollection.AppHeaders =
                    database.AppHeaders.Where(x => x.AppCollectionId == appcollection.Id)
                    .OrderBy(x => x.Name).ToList();
                item.AppBools =
                    database.AppBools.Where(x => x.AppItemId == item.Id).ToList();
                item.AppDates =
                    database.AppDates.Where(x => x.AppItemId == item.Id).ToList();
                item.AppNumbers =
                    database.AppNumbers.Where(x => x.AppItemId == item.Id).ToList();
                item.AppStrings =
                    database.AppStrings.Where(x => x.AppItemId == item.Id).ToList();
                item.AppTexts =
                    database.AppTexts.Where(x => x.AppItemId == item.Id).ToList();

                item.AppTags =
                    database.AppTags.Where(x => x.AppItemId == item.Id).ToList();

                ViewBag.TagString = TagListToString(item.AppTags);
                ViewBag.Item = item;
                ViewBag.Collection = appcollection;
                ViewBag.ReturnUrl = returnUrl;
                if (item != null)
                    return View();
            }
                                               
            return NotFound();
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> EditItem(AppItem appItem, 
           string TagString="", string returnUrl="")
        {
            AppUser user = await _userManager.FindByNameAsync(User.Identity.Name);
            AppCollection appcollection = await
                database.AppCollections.FindAsync(appItem.AppCollectionId);

            database.AppTags.Where(x => x.AppItemId == appItem.Id)
                .ToList().ForEach(x=> database.AppTags.Remove(x));

            if ((TagString != null) && (TagString != ""))
            {
                foreach (var x in TagsToList(TagString))
                {
                    AppTag appTag = new AppTag();
                    appTag.AppItemId = appItem.Id;
                    appTag.Text = x;
                    appItem.AppTags.Add(appTag);
                }
            }
            
            if ((appcollection.Id != null) && ((appcollection.AppUserId == user.Id)
                    || User.IsInRole("Admin")))
            {
                database.AppItems.Update(appItem);
                database.SaveChanges();
                TagsCloudExec();                
                
            }
            return Redirect(returnUrl);
        }
        
        [HttpGet]
        [Authorize]
        [ActionName("DeleteCollection")]
        public async Task<IActionResult> ConfirmDelete(string id)
        {
            if (id != null)
            {
                AppCollection appcollection = await database.AppCollections.FindAsync(id);

                if (appcollection != null)
                    return View(appcollection);
            }
            return NotFound();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteCollection(string id)
        {
            if (id != null)
            {
                AppCollection appcollection = await database.AppCollections.FindAsync(id);
                AppUser user = await _userManager.FindByNameAsync(User.Identity.Name);

                if ((appcollection.Id != null) && ((appcollection.AppUserId == user.Id)
                        || User.IsInRole("Admin")))
                {
                    database.AppCollections.Remove(appcollection);
                    await database.SaveChangesAsync();
                    return RedirectToAction("MyPage");
                }
            }
            return NotFound();
        }

        [Authorize]
        public async Task<IActionResult> DeleteItem(string id)
        {
            if (id != null)
            {
                AppItem item = await database.AppItems.FindAsync(id);
                AppCollection appcollection = await database.AppCollections.FindAsync(item.AppCollectionId);
                AppUser user = await _userManager.FindByNameAsync(User.Identity.Name);

                if ((item.Id != null) && (appcollection.Id != null) && ((appcollection.AppUserId == user.Id)
                        || User.IsInRole("Admin")))
                {
                    database.AppItems.Remove(item);
                    await database.SaveChangesAsync();
                    return RedirectToAction("EditCollectionItems", new { id = item.AppCollectionId });
                }
            }
            return NotFound();
        }

        public string GetCulture(string code = "")
        {
            if (!String.IsNullOrEmpty(code))
            {
                CultureInfo.CurrentCulture = new CultureInfo(code);
                CultureInfo.CurrentUICulture = new CultureInfo(code);
            }
            return $"CurrentCulture:{CultureInfo.CurrentCulture.Name}, CurrentUICulture:{CultureInfo.CurrentUICulture.Name}";
        }

        public IActionResult SetTheme(string data)
        {
            CookieOptions cookies = new CookieOptions();
            cookies.Expires = DateTime.Now.AddDays(30);

            Response.Cookies.Append("theme", data, cookies);
            return Ok();
        }

        public IActionResult SetMobileDesktop(string data)
        {
            CookieOptions cookies = new CookieOptions();
            cookies.Expires = DateTime.Now.AddDays(30);

            Response.Cookies.Append("mobileDesktop", data, cookies);
            return Ok();
        }

        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );
            return LocalRedirect(returnUrl);
        }
        [HttpPost]

        private List<AppUser> Act(string id)
        {
            List<AppUser> _actionList = new List<AppUser>();
            string[] a = id.Split(',');

            foreach (var n in a)
            {
                _actionList.Add(_userManager.Users.ToList().Find(x => x.Id == n));
            }
            return _actionList;
        }
        
        [Authorize(Roles="Admin")]
        public IActionResult Details(string ErrorMessage=null)
        {
            if (ErrorMessage != null)
                ModelState.AddModelError("",ErrorMessage);
            return View(_userManager.Users.ToList());
        }

        public IActionResult Smde()
        {           
            return View();
        }
        public IActionResult Index()
        {         
            ViewBag.IsMobile = IsMobile(Request);             
            var query = from AppItem in database.AppItems
                         where AppItem.CreateDate > DateTime.Now.AddDays(-3)
                         select new
                         {
                             Id = AppItem.Id,
                             Name = AppItem.Name,
                             CreateDate = AppItem.CreateDate,
                         };

             List<AppItemSearch> L = new List<AppItemSearch>();
             foreach (var x in query)
             {
                 L.Add(new AppItemSearch
                 {
                     Id = x.Id,
                     Name = x.Name,
                 });
             }

             ViewBag.ItemList = L;
             var query2 = from AppItem in database.AppItems
                          group AppItem by AppItem.AppCollectionId into x
                          select new
                          {
                              Id = x.Key, Count = x.Count()
                          };
             var r = query2.Join(database.AppCollections,
                 x => x.Id, y => y.Id,
                 (x, y) => new { Id = x.Id, Name = y.Name, Count = x.Count }
                 ).OrderByDescending(n=>n.Count);
             List < AppCollectionSearch > L2 = new List<AppCollectionSearch>();
             for (int i=0; (i<3)&&(i<r.ToList().Count()); i++)
             {
                  L2.Add(new AppCollectionSearch
                  {
                         Id = r.ToList()[i].Id,                      
                         Name = r.ToList()[i].Name,
                         Details = Convert.ToString(r.ToList()[i].Count),                        
                  });
             }
            ViewBag.CollectionList = L2.ToList();            
            ViewBag.TagList = database.AppTagCloudRecords.ToList();
            return View();
        }
        
        public async Task<IActionResult> Search(string requestString, SearchData model)
        {
            if (model.SearchText == null)
            {
                if (requestString != null)
                    model.SearchText = requestString;
                else
                    model.SearchText = "";
            }
            await RunQueryAsync(model);
            return View(model); 
        }
        [Authorize]
        public async Task<IActionResult> MyPage()
        {
            ViewBag.Themes = GetAppThemes();
            AppUser user = await _userManager.FindByNameAsync(User.Identity.Name);                        
            IQueryable<AppCollection> collections = database.AppCollections
                .Where(p => (p.AppUserId == user.Id));
            ViewBag.Collections = collections.ToList();                        
            return View();
        }
        
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(string id)
        {            
            try
            {                
                foreach (var user in Act(id))
                {
                    if (database.AppCollections.ToList().Exists(x => x.AppUserId == user.Id))
                        return RedirectToAction("Details",
                            new { ErrorMessage = "Delete this user collections first" });
                    await _userManager.UpdateSecurityStampAsync(user);
                    await _userManager.DeleteAsync(user);
                }
            }
            catch {                
            }
            return RedirectToAction("Details");                  
        }

        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Block(string id)
        {                                                
             foreach (var user in Act(id))
             {
                    user.Blocked = true;
                    await _userManager.UpdateAsync(user);
                    await _userManager.UpdateSecurityStampAsync(user);
             }                                       
            return RedirectToAction("Details");            
        }

        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Unblock(string id)
        {          
            try
            {
                foreach (var user in Act(id))
                {
                    user.Blocked = false;
                    await _userManager.UpdateAsync(user);
                }
            }
            catch { }
            return RedirectToAction("Details");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
