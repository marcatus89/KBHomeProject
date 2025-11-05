
export function stopProgressBar() {
  NProgress.done();
}


export function setupNavigationProgressListener() {
  document.body.addEventListener('click', function (event) {

    const anchor = event.target.closest('a');

  
    if (anchor && anchor.href && anchor.target !== '_blank' && anchor.hasAttribute('href') && !anchor.getAttribute('href').startsWith('#')) {
      NProgress.start();
    }
  });
}
export function printPage() {
  window.print();
}
