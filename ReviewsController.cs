using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BlueRibbonsReview.Models;
using Microsoft.AspNet.Identity;

namespace BlueRibbonsReview.Controllers
{
    public class ReviewsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Reviews       
        public ActionResult Index(string sortOrder)
        {
            ViewBag.ReviewProductSortParm = String.IsNullOrEmpty(sortOrder) ? "reviewProductDesc" : "";
            ViewBag.ReviewRatingSortParm = sortOrder == "reviewRating" ? "reviewRatingDesc" : "reviewRating";
            ViewBag.ReviewDateSortParm = sortOrder == "reviewDate" ? "reviewDateDesc" : "reviewDate";
            ViewBag.Title = "Product Reviews";

            var reviews = db.Reviews.Include(r => r.ApplicationUser);
            
            switch (sortOrder)
            {
                case "reviewProductDesc":
                    reviews = reviews.OrderByDescending(s => s.CampaignId);
                    break;
                case "reviewRating":
                    reviews = reviews.OrderBy(s => s.ProductRating);
                    break;
                case "reviewRatingDesc":
                    reviews = reviews.OrderByDescending(s => s.ProductRating);
                    break;
                case "reviewDate":
                    reviews = reviews.OrderBy(s => s.ReviewDate);
                    break;
                case "reviewDateDesc":
                    reviews = reviews.OrderByDescending(s => s.ReviewDate);
                    break;
                default:
                    reviews = reviews.OrderBy(s => s.CampaignId);
                    break;
            }
            return View(reviews.ToList());
        }

        // GET: Relevant Reviews on the Campaigns the Seller has created.   
        public ActionResult SellerReviewIndex()
        {
            string UserId = HttpContext.User.Identity.GetUserId();
            var campaignReviews = db.Reviews.Where(x => x.Campaign.ApplicationUser.Id.ToString() == UserId);
            ViewBag.Title = "Reviews of Your Products";

            return View("Index", campaignReviews);
        }

        // GET: Reviews the buyer has created
        // Same expression as seen on Campaign Controller
        public ActionResult ReviewIndex()
        {
            string UserId = HttpContext.User.Identity.GetUserId();       
            var campaignReviews = db.Reviews.Where(x => x.UserId.ToString() == UserId);
            ViewBag.Title = "Products You've Reviewed";

            return View("Index", campaignReviews);
        }

        // GET: Reviews/Details/5
        public ActionResult Details(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Review review = db.Reviews.Find(id);
            if (review == null)
            {
                return HttpNotFound();
            }
            return View(review);
        }

        // GET: Reviews/Create
        public ActionResult Create(int? id, string name)
        {
            string UserId = HttpContext.User.Identity.GetUserId();
            string Campaign = "";

            if (id==null)
            {
                // Create raw html for each item in db.Campaigns and pass to ViewBag
                Campaign = "<div class=\"dropdown filterArea\">" +
                           "<input class=\"dropdown-toggle\" id=\"campaignReview\" type=\"text\" data-toggle=\"dropdown\" aria-haspopup=\"true\" aria-expanded=\"true\" placeholder=\"Select A Campaign\">" +
                           "<ul class=\"dropdown-menu\">\"";
                foreach (var item in db.Campaigns)
                {
                    Campaign += string.Format(
                        "<li id=\"{0}\"><a><img src=\"{1}\" class=\"imgDisplay\" />" +
                        "<p class=\"imageText\" id=\"dropdownName\">{2}</p></a>" +
                        "</li>",
                        item.CampaignID, item.ImageUrL, item.Name);
                }
                Campaign += "</ul>" +
                            "<br />" +                            
                            "</div>";
            }
            else
            {
                // Create raw html for the item specified in parameters, send CampaignId received to controller
                Campaign = string.Format("<input type='text' value=\"{0}\" readonly", name);               
                ViewBag.CampaignReviewId = id;


            }          

            ViewBag.UserId = new SelectList(new[] { UserId });
            ViewBag.Campaign = Campaign;
            Review review = new Review();
            review.ReviewDate = DateTime.Today;

            return View(review);
        }

        // POST: Reviews/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ProductRating,ReviewDetails,CampaignId")] Review review)
        {
            try
            {
                if (string.IsNullOrEmpty(review.ReviewDetails))
                {
                    ModelState.AddModelError("ReviewDetails", "Please write a review.");
                }
                if (review.CampaignId == null)
                {
                    ModelState.AddModelError("CampaignId", "Please select a campaign.");
                }
                if (ModelState.IsValid)
                {
                    review.ReviewID = Guid.NewGuid();
                    review.ReviewDate = DateTime.Today;
                    string userId = User.Identity.GetUserId();
                    review.UserId = userId;
                    review.ApplicationUser = db.Users.Where(u => u.Id == userId).Single();
                    review.ProductRating = Int32.Parse(Request.Form["rating"]);
                    db.Reviews.Add(review);
                    db.SaveChanges();

                    // debug purposes
                    System.Diagnostics.Debug.WriteLine(review.ReviewID);
                    System.Diagnostics.Debug.WriteLine(review.ReviewDate);
                    System.Diagnostics.Debug.WriteLine(review.ProductRating);
                    System.Diagnostics.Debug.WriteLine(review.ReviewDetails);
                    System.Diagnostics.Debug.WriteLine(review.UserId);
                    System.Diagnostics.Debug.WriteLine(review.CampaignId);

                    return RedirectToAction("Index");
                }
            }
            catch (DataException)
            {
                ModelState.AddModelError("", "Unable to save changes. Please try again and if the problem persists, see your System Administrator.");
            }

            // Create raw html for each item in db.Campaigns and pass to ViewBag
            string Campaign = "";
            Campaign = "<div class=\"dropdown filterArea\">" +
                       "<input class=\"dropdown-toggle\" id=\"campaignReview\" type=\"text\" data-toggle=\"dropdown\" aria-haspopup=\"true\" aria-expanded=\"true\" placeholder=\"Select A Campaign\">" +
                       "<ul class=\"dropdown-menu\">\"";
            foreach (var item in db.Campaigns)
            {
                Campaign += string.Format(
                    "<li id=\"{0}\"><a><img src=\"{1}\" class=\"imgDisplay\" />" +
                    "<p class=\"imageText\" id=\"dropdownName\">{2}</p></a>" +
                    "</li>",
                    item.CampaignID, item.ImageUrL, item.Name);
            }
            Campaign += "</ul>" +
                        "<br />" +
                        "</div>";
            ViewBag.Campaign = Campaign;

            return View(review);
        }
            
        // GET: Reviews/Edit/5
        public ActionResult Edit(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Review review = db.Reviews.Find(id);
            if (review == null)
            {
                return HttpNotFound();
            }
            return View(review);
        }

        // POST: Reviews/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.

        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public ActionResult EditPost(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Review review = db.Reviews.Find(id);
            if (TryUpdateModel(review, "", new string[] { "ReviewDate", "ProductRating", "ReviewDetails", "UserId" }))
            {
                try
                {
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                catch (DataException/*dex*/)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again and if the problem continues contact your System Administrator.");
                }
            }
            return View(review);
        }

        // GET: Reviews/Delete/5
        public ActionResult Delete(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Review review = db.Reviews.Find(id);
            if (review == null)
            {
                return HttpNotFound();
            }
            return View(review);
        }

        // POST: Reviews/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(Guid id)
        {
            Review review = db.Reviews.Find(id);
            db.Reviews.Remove(review);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
