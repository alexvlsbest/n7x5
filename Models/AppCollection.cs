using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace N7.Models
{
    public class A
    {
        public string Name { get; set; }
        public List<string> Items { get; set; } = new();
    }

    public class AppCollectionModel
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Details { get; set; }
        public string Image { get; set; }
        public List<AppItem> AppItems { get; set; } = new();
        public List<AppHeader> AppHeaders { get; set; } = new();
        public string AppUserId { get; set; }        
        public string AppThemeId { get; set; }
        
    }


    public class  AppCollection 
    {        
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }                
        public string Name { get; set; }
        public string Details { get; set; }
        public string Image { get; set; }        
        public List<AppItem> AppItems { get; set; } = new();
        public List<AppHeader> AppHeaders { get; set; } = new();
        public string AppUserId { get; set; }
        public AppUser AppUser { get; set; }
        public string AppThemeId { get; set; }
        public AppTheme AppTheme { get; set; }
    }

    public class AppItem
    {             
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }                
        public string Name { get; set; }
        public DateTime CreateDate { get; set; }        
        public List<AppTag> AppTags { get; set; } = new();
        public List<AppComment> AppComments { get; set; } = new();
        public List<AppLike> AppLikes { get; set; } = new();
        
        public List<AppBool> AppBools { get; set; } = new();
        public List<AppDate> AppDates { get; set; } = new();
        public List<AppNumber> AppNumbers { get; set; } = new();        
        public List<AppString> AppStrings { get; set; } = new();
        public List<AppText> AppTexts { get; set; } = new();
        public string AppCollectionId { get; set; }
        public AppCollection AppCollection { get; set; }
    }


    public class AppLike
    {        
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }        
        public DateTime Time { get; set; }
        public string AppItemId { get; set; }
        public AppItem AppItem { get; set; }
        public string AppUserId { get; set; }
        public AppUser AppUser { get; set; }        
    }
    
    public class AppComment
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public string Text { get; set; }
        public DateTime Time { get; set; }
        public string AppItemId { get; set; }
        public AppItem AppItem { get; set; }
        public string AppUserId { get; set; }
        public AppUser AppUser { get; set; }               
    }

    [Table("AppBools")]
    public class AppBool : AppField<bool> { }
    [Table("AppDates")]
    public class AppDate : AppField<DateTime> { }
    [Table("AppNumbers")]
    public class AppNumber : AppField<double> { }
    [Table("AppStrings")]
    public class AppString : AppField<string> { }
    [Table("AppTexts")]
    public class AppText : AppField<string> { }
                   
    public class AppField<T>
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public T Value { get; set; }
        public string AppHeaderId { get; set; }
        public AppHeader AppHeader { get; set; }
        public string AppItemId { get; set; }
        public AppItem AppItem { get; set; }
    }
    
    public class AppHeader
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public string Name { get; set; }
        public string DataType { get; set; }
        public string AppCollectionId { get; set; }
        public AppCollection AppCollection { get; set; }
    }

    public class AppTag
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public string Text { get; set; }
        public string AppItemId { get; set; }
        public AppItem AppItem { get; set; }
    }

    public class AppTheme
    {        
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public string Name { get; set; }
    }
    
    public class AppTagCloudRecord
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public string Text { get; set; }
        public double Power { get; set; }
    }

    public class SearchData
    {
        // The text to search for.
        public string SearchText { get; set; }

        // The list of results.

        public SearchResults<AppItem> ResultList;
    }
    
    public class AppItemSearch
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Tags { get; set; }
        public string AppCollectionId { get; set; }
        public string CollectionName { get; set; }
        public string AppUserId { get; set; }
        public string UserName { get; set; }
        public string AppThemeId { get; set; }
        public string ThemeName { get; set; }
        public DateTime CreateDate { get; set; }

    }

    public class AppCollectionSearch
    {
        public string Id { get; set; }
        public string Name { get; set; }     
        public string Details { get; set; }
        public string AppUserId { get; set; }
        public string UserName { get; set; }
        public string AppThemeId { get; set; }
        public string AppThemeName { get; set; }
    }

}
