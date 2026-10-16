(() => {
  'use strict';
  const links = [...document.querySelectorAll('.study-nav a')];
  const visibility = new Map();
  const observer = new IntersectionObserver(entries => {
    for (const entry of entries) {
      visibility.set(entry.target.id, entry.intersectionRatio);
    }
    const active = [...visibility].sort((a,b) => b[1]-a[1])[0]?.[0] || 'cyclical';
    document.body.dataset.activeStudy = active;
    links.forEach(link => {
      const selected = link.hash === '#' + active;
      link.classList.toggle('active', selected);
      if (selected) link.setAttribute('aria-current', 'location');
      else link.removeAttribute('aria-current');
    });
  }, { threshold: [0,.15,.35,.5,.65,.85,1] });
  document.querySelectorAll('.study').forEach(study => observer.observe(study));

})();
