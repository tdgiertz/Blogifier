export function initialize(lastItemIndicator, componentInstance) {
  const options = {
    //root: findClosestScrollContainer(lastItemIndicator),
    rootMargin: '0px',
    threshold: 0,
  };

  const observer = new IntersectionObserver(async (entries) => {
    for (const entry of entries) {
      if (entry.isIntersecting) {
        do {
          await componentInstance.invokeMethodAsync("LoadMoreItems");
        } while (isInViewport(lastItemIndicator));
      }
    }
  }, options);

  observer.observe(lastItemIndicator);

  return {
    dispose: () => dispose(observer),
    onNewItems: () => {
      observer.unobserve(lastIndicator);
      observer.observe(lastIndicator);
    },
  };
}

function isInViewport(element) {
  const rect = element.getBoundingClientRect();
  return (
    rect.top >= 0 &&
    rect.left >= 0 &&
    rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
    rect.right <= (window.innerWidth || document.documentElement.clientWidth)
  );
}

function dispose(observer) {
  observer.disconnect();
}

function findClosestScrollContainer(element) {
  while (element) {
    const style = getComputedStyle(element);
    if (style.overflowY !== 'visible') {
      return element;
    }
    element = element.parentElement;
    console.log(element);
  }
  return null;
}
