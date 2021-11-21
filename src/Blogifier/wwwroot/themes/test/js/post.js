let _isLoading = false;

function load() {
  if (!_isLoading) {
    _isLoading = true;

    const pagingUrl = document.getElementById('paging-url');
    const container = document.getElementById('post-container');

    fetch(pagingUrl.value)
      .then(function (response) {
        return response.text();
      })
      .then(function (html) {
        if (html != '') {

          pagingUrl.remove();
          document.getElementById('load-posts-container').remove();
          container.insertAdjacentHTML('beforeend', html);
          setupClickEvent();
        }
        _isLoading = false;
      });
  }
}

setupClickEvent();

function setupClickEvent() {
  const loadPostsButton = document.querySelector('#load-posts-container button');

  if (loadPostsButton) {
    loadPostsButton.addEventListener('click', function (e) {
      load();
    });
  }
}
