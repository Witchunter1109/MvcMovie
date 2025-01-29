using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MvcMovie.Data;
using MvcMovie.Models;

namespace MvcMovie.Controllers
{
    public class MoviesController : Controller
    {
        private readonly MvcMovieContext _context;

        public MoviesController(MvcMovieContext context)
        {
            _context = context;
        }

        // GET: Movies
        public async Task<IActionResult> Index(string movieGenre, string searchString)
        {
            if (_context.Movie == null)
            {
                return Problem("Entity set 'MvcMovieContext.Movie' is null.");
            }

            IQueryable<string> genreQuery = _context.Movie
                .OrderBy(m => m.Genre)
                .Select(m => m.Genre)
                .Distinct();

            var movies = _context.Movie
                .Include(m => m.Reviews)
                .AsQueryable();

            foreach (var movie in movies)
            {
                movie.Rating = movie.Reviews.Any() ? movie.Reviews.Average(r => r.Rating).ToString("0.0") : "Brak";
            }

            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(searchString))
            {
                movies = movies.Where(s => s.Title.ToUpper().Contains(searchString.ToUpper()));
            }

            if (!string.IsNullOrEmpty(movieGenre))
            {
                movies = movies.Where(x => x.Genre == movieGenre);
            }

            var movieList = await movies.ToListAsync();

            var movieGenreVM = new MovieGenreViewModel
            {
                Genres = new SelectList(await genreQuery.ToListAsync()),
                Movies = await movies.ToListAsync()
            };

            return View(movieGenreVM);
        }


        // GET: Movies/Details/
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie
                .Include(m => m.VideoFiles) // Załaduj powiązane pliki wideo
                .Include(m => m.Reviews)  // Załaduj recenzje dla filmu
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }


        // GET: Movies/Create
        public IActionResult Create()
        {
            return View();
        }

        // GET: Movies/Edit/
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie.FindAsync(id);
            if (movie == null)
            {
                return NotFound();
            }
            return View(movie);
        }

        // POST: Movies/Edit/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,ReleaseDate,Genre,Rating")] Movie movie, IFormFile? fileUpload)
        {
            if (id != movie.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingMovie = await _context.Movie
                        .Include(m => m.VideoFiles)
                        .FirstOrDefaultAsync(m => m.Id == id);

                    if (existingMovie == null)
                    {
                        return NotFound();
                    }

                    existingMovie.Title = movie.Title;
                    existingMovie.ReleaseDate = movie.ReleaseDate;
                    existingMovie.Genre = movie.Genre;
                    existingMovie.Rating = movie.Rating;

                    if (fileUpload != null && fileUpload.Length > 0)
                    {
                        var oldVideo = existingMovie.VideoFiles.FirstOrDefault();
                        if (oldVideo != null)
                        {
                            var oldFilePath = Path.Combine("wwwroot", oldVideo.FilePath.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                                Console.WriteLine($"Usunięto stary plik: {oldFilePath}");
                            }

                            _context.VideoFile.Remove(oldVideo);
                        }

                        var newFilePath = Path.Combine("wwwroot/videos", fileUpload.FileName);
                        using (var stream = new FileStream(newFilePath, FileMode.Create))
                        {
                            await fileUpload.CopyToAsync(stream);
                        }
                        Console.WriteLine($"Dodano nowy plik: {newFilePath}");

                        var videoFile = new VideoFile
                        {
                            FileName = fileUpload.FileName,
                            FilePath = $"/videos/{fileUpload.FileName}",
                            MovieId = existingMovie.Id
                        };

                        _context.VideoFile.Add(videoFile);
                    }

                    _context.Update(existingMovie);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MovieExists(movie.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }


        // GET: Movies/Delete/
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var movie = await _context.Movie
                .FirstOrDefaultAsync(m => m.Id == id);
            if (movie == null)
            {
                return NotFound();
            }

            return View(movie);
        }

        // POST: Movies/Delete/
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movie.FindAsync(id);
            if (movie != null)
            {
                _context.Movie.Remove(movie);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MovieExists(int id)
        {
            return _context.Movie.Any(e => e.Id == id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,ReleaseDate,Genre,Price,Rating")] Movie movie, IFormFile? fileUpload)
        {
            if (ModelState.IsValid)
            {
                Console.WriteLine("Dodawanie filmu do bazy danych...");
                _context.Add(movie);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Film '{movie.Title}' został zapisany w bazie danych z ID: {movie.Id}");

                if (fileUpload != null && fileUpload.Length > 0)
                {
                    Console.WriteLine($"Otrzymano plik: {fileUpload.FileName} ({fileUpload.Length} bajtów)");

                    // Ścieżka do folderu wwwroot/videos
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "videos");

                    // Sprawdzenie czy folder istnieje
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Console.WriteLine("Folder 'wwwroot/videos' nie istnieje. Tworzę nowy...");
                        Directory.CreateDirectory(uploadsFolder);
                        Console.WriteLine("Folder utworzony.");
                    }

                    // Generowanie unikalnej nazwy pliku
                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileUpload.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    try
                    {
                        // Kopiowanie pliku na serwer
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await fileUpload.CopyToAsync(stream);
                        }

                        Console.WriteLine($"Plik został zapisany na serwerze: {filePath}");

                        // Tworzenie wpisu w bazie danych
                        var videoFile = new VideoFile
                        {
                            FileName = uniqueFileName,
                            FilePath = $"/videos/{uniqueFileName}", // Ścieżka względna dla przeglądarki
                            MovieId = movie.Id
                        };

                        _context.VideoFile.Add(videoFile);
                        await _context.SaveChangesAsync();
                        Console.WriteLine("Ścieżka pliku została zapisana w bazie danych.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Błąd podczas zapisywania pliku: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Brak pliku do przesłania.");
                }

                return RedirectToAction(nameof(Index));
            }

            Console.WriteLine("ModelState jest niepoprawny. Zwrot do formularza.");
            return View(movie);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int rating, string comment, int movieId)
        {
            var review = new Review
            {
                Rating = rating,
                Comment = comment,
                MovieId = movieId
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = movieId });
        }

        public async Task<IActionResult> Statistics()
        {
            var movies = await _context.Movie
                .Include(m => m.Reviews)
                .ToListAsync();

            var movieStats = movies.Select(m => new
            {
                Title = m.Title,
                ReviewCount = m.Reviews.Count,
                AverageRating = m.Reviews.Any() ? m.Reviews.Average(r => r.Rating).ToString("0.0") : "Brak"
            }).ToList();

            return View(movieStats);
        }


    }
}
