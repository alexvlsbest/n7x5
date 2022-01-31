using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace N7.Models
{
    public class AppUser : IdentityUser
    {
        public bool Blocked { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime LastLoginDate { get; set; }

        public List<AppCollection> AppCollections { get; set; } = new();

    }
}


/*
select[dbo].[AppItems].Id, [dbo].[AppItems].[Name],
[dbo].[AppCollections].[Name] as CollectionName,
details,[dbo].[AppThemes].[Name] as ThemeName,
UserName, AppTags.[Text], [dbo].[AppComments].[text] as commenttext,
appHeaders.[name] as hname, 
appstrings.[value] as vs,
apptexts.[value] as vt
from[dbo].[AppItems]
join[dbo].[AppCollections]
on[dbo].[AppItems].[AppCollectionId] = [dbo].[AppCollections].Id
join[dbo].[AppThemes]
on[dbo].[AppCollections].AppThemeId = [dbo].[AppThemes].Id
left join[dbo].[AppHeaders]
on[dbo].[AppHeaders].AppCollectionId = AppCollections.Id
left join[dbo].[AppTags]
on[dbo].[AppTags].AppItemId = AppItems.Id
left join[dbo].[AppStrings]
on ([dbo].[AppStrings].AppItemId = AppItems.Id
and[dbo].[AppStrings].AppHeaderId = AppHeaders.Id)
left join[dbo].[AppTexts]
on ([dbo].[AppTexts].AppItemId = AppItems.Id
and[dbo].[AppTexts].AppHeaderId = AppHeaders.Id)
join[dbo].[AspNetUsers]
on[dbo].[AppCollections].AppUserId = [dbo].[AspNetUsers].Id
left join[dbo].[AppComments]
on[dbo].[AppItems].Id = AppComments.AppItemId
*/