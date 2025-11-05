
export function initCarousel(selector) {
  var myCarousel = document.querySelector(selector);
  if (myCarousel) {
    var carousel = new bootstrap.Carousel(myCarousel, {
      interval: 5000, 
      wrap: true
    });
  }
}
