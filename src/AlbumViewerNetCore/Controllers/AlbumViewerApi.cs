using System.Runtime;
using AlbumViewerBusiness;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections;
using System.Text;
using Westwind.Utilities;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

//using Westwind.Utilities;


// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace AlbumViewerAspNet5
{
    [ApiExceptionFilter]
    [EnableCors("CorsPolicy")]
    public class AlbumViewerApiController : Controller
    {
        AlbumViewerContext context;
        IServiceProvider serviceProvider;
       
        public AlbumViewerApiController(AlbumViewerContext ctx, IServiceProvider svcProvider)
        {
            context = ctx;
            serviceProvider = svcProvider;
        }
        [HttpGet]
        [Route("api/helloworld")]
        public object HelloWorld(string name = null)
        {
            //return "Hello " + name + ". Time is: " + DateTime.Now;
            if (string.IsNullOrEmpty(name))
                name = "Johnny Doe";

            return new { helloMessage = "Hello!  " + name + ". Time is: " + DateTime.Now };
        }

        [HttpGet]
        [Route("api/albums")]
        public async Task<IEnumerable<Album>> Albums(int page = -1, int pageSize = 15)
        {
            IQueryable<Album> enumResult = context.Albums
                .Include(ctx => ctx.Tracks)
                .Include(ctx => ctx.Artist)
                .OrderBy(alb => alb.Title);

            if (page > 0)
            {
                enumResult = enumResult
                                .Skip( (page - 1)  * pageSize)
                                .Take(pageSize);
            }

            //return await enumResult.ToListAsync();

            // faster RTM
            return enumResult;      
        }

        [HttpGet("api/album/{id:int}")]
        public async Task<Album> Album(int id)
        {
            // var contextAlbums = new AlbumViewerContext(serviceProvider);
            var albums = context.Albums
                                .Include(ctx => ctx.Artist)
                                .Include(ctx => ctx.Tracks)
                                .Where(alb => alb.Id == id);

            return albums.FirstOrDefault();
            //return await albums.FirstOrDefaultAsync();
        }

        [HttpPost("api/album")]
        public async Task<Album> Album([FromBody] Album postedAlbum)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
                throw new ApiException("You have to be logged in to modify data", 401);

            if (!ModelState.IsValid)            
                throw new ApiException("Model binding failed.",500);

            var albumRepo = new AlbumRepository(context);
            return await albumRepo.SaveAlbum(postedAlbum);
        }


        [HttpGet]
        [Route("api/artists")]
        public async Task<IEnumerable> Artists()
        {
            var artists = await context.Artists
                .OrderBy(art => art.ArtistName)
                .Select(art => new ArtistWithAlbum()
                {
                    ArtistName = art.ArtistName,
                    Description = art.Description,
                    ImageUrl = art.ImageUrl,
                    Id = art.Id,
                    AmazonUrl = art.AmazonUrl,
                    AlbumCount = context.Albums.Count(alb => alb.ArtistId == art.Id)
                })
                .ToAsyncEnumerable()
                .ToList();

            return artists;
        }

        [HttpGet("api/artist/{id:int}")]
        public async Task<object> Artist(int id)
        {
            var artist = context.Artists
                .FirstOrDefault(art => art.Id == id);

            if (artist == null)
                throw new ApiException("Invalid artist id.", 404);

            var albums = await context.Albums
                            .Where(alb => alb.ArtistId == id)
                            .ToAsyncEnumerable().ToList();

            return new ArtistResponse()
            {
                Artist = artist,
                Albums = albums
            };
        }

        [HttpPost("api/artist")]
        public async Task<ArtistResponse> Artist([FromBody] Artist postedArtist)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
                throw new ApiException("You have to be logged in to modify data",401);

            var db = new AlbumRepository(context);
            var artist = await db.SaveArtist(postedArtist);

            if (artist == null)
                throw new ApiException("Unable to save artist.");

            return new ArtistResponse()
            {
                Artist = artist,
                Albums = await db.Context.Albums
                    .Include(a=> a.Tracks)
                    .Include(a=> a.Artist)            
                    .Where(a => a.ArtistId == artist.Id).ToListAsync()
            };
        }

        [HttpGet("api/artistlookup")]
        public async Task<IEnumerable<object>> ArtistLookup(string search = null)
        {
            if (string.IsNullOrEmpty(search))
                return new List<object>();
            
            var db = new AlbumRepository(context);

            var term = search.ToLower();
            var artists = await db.Context.Artists
                                  .Where(a => a.ArtistName.ToLower().StartsWith(term))
                                  .Select(a => new
                                  {
                                      name = a.ArtistName,
                                      id = a.ArtistName
                                  })
                                  .ToAsyncEnumerable().ToList();

            return artists;
        }


        [HttpGet("api/deletealbum/{id:int}")]
        public async Task<bool> DeleteAlbum(int id)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
                throw new ApiException("You have to be logged in to modify data", 401);


            var db = new AlbumRepository(context);
            return await db.DeleteAlbum(id);
        }



        [HttpDelete("api/artist/id")]
        public async Task<bool> DeleteArtist(int id)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
                throw new ApiException("You have to be logged in to modify data", 401);


            var db = new AlbumRepository(context);
            return await db.DeleteArtist(id);
        }


        [HttpGet]
        public async Task<string> DeleteAlbumByName(string name)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
                throw new ApiException("You have to be logged in to modify data", 401);

            var repo = new AlbumRepository(context);

            var pks = await context.Albums.Where(alb => alb.Title == name).Select(alb => alb.Id).ToAsyncEnumerable().ToList();

            StringBuilder sb = new StringBuilder();
            foreach (int pk in pks)
            {
                bool result = await repo.DeleteAlbum(pk);
                if (!result)
                    sb.AppendLine(repo.ErrorMessage);
            }

            return sb.ToString();
        }
    }

    #region Custom Responses
    public class ArtistResponse
    {
        public Artist Artist { get; set; }

        public List<Album> Albums { get; set; }
    }

    public class ArtistWithAlbum : Artist
    {
        public int AlbumCount { get; set; }
    }
    #endregion
}

