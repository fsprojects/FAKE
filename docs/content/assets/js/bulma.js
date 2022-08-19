function generateTableOfContent() {
    const menu = document.querySelector('aside.menu');
    if (menu == null) return;

    const content = document.querySelector('#content');
    if (content == null) return;

    const headings = content.querySelectorAll('h2,h3,h4,h5,h6,h7');
    if (headings.length > 0) {
        let menuHtml = '<ul class="menu-list">';
        let headingNumber;
        let levels = 0;
        let headersCount = 0;

        const header = content.querySelector('h1');
        if (header != null)
            menuHtml += `<p class="menu-label">${header.innerText}</p>`;

        for (let i = 0; i < headings.length; i++) {
            const heading = headings[i];
            const headingParent = heading.parentElement;

            if (headingParent != null && headingParent.classList.contains('alert')) continue;

            const currentHeadingCount = parseInt(heading.tagName.match(/[0-9]+/g), 10);
            if (isNaN(currentHeadingCount)) continue;

            const anchor = heading.querySelector('a');
            const href = anchor != null ? anchor.href : "";
            if (href === "") continue;

            if (headingNumber == null) {
                menuHtml += `<li><a href="${href}">${heading.innerText}</a>`;
            } else if (currentHeadingCount > headingNumber) {
                // increase level
                menuHtml += `<ul class="menu-list"><li><a href="${href}">${heading.innerText}</a>`;
                levels++;
            } else if (currentHeadingCount < headingNumber) {
                // decrease level, could go down more than one level (say a previous section ends with h4 and a new section starts with h2)
                let headingNumDif = headingNumber - currentHeadingCount;
                for (let d = 0; d < headingNumDif; d++) { menuHtml += "</li></ul>" }
                menuHtml += `<li><a href="${href}">${heading.innerText}</a>`;
                levels-=headingNumDif;
            } else if (currentHeadingCount == headingNumber) {
                // same level
                menuHtml += `</li><li><a href="${href}">${heading.innerText}</a>`;
            }

            headingNumber = currentHeadingCount;
            headersCount++;
        }

        menuHtml += '</li>';
        for (; levels > 0; levels--) {
            menuHtml += '</ul></li>';
        }
        menuHtml += '</ul>';

        menu.innerHTML = menuHtml;
        if (headersCount > 0) {
            menu.style.display = '';
        } else {
            hideMenu(menu);
        }
    } else {
        hideMenu(menu);
    }
}

function hideMenu(menu) {
    menu.style.display = 'none';
    if (menu.previousElementSibling != null) {
        menu.previousElementSibling.classList.remove('is-9-desktop');
        menu.previousElementSibling.classList.add('is-12-desktop');
    }
}

function registerNavbarBurger() {
    const $navbarBurgers = Array.prototype.slice.call(document.querySelectorAll('.navbar-burger'), 0);

    if ($navbarBurgers.length > 0) {
        $navbarBurgers.forEach(el => {

            el.addEventListener('click', () => {
                const target = el.dataset.target;
                const $target = document.getElementById(target);

                el.classList.toggle('is-active');
                $target.classList.toggle('is-active');

            });
        });
    }
}

document.addEventListener('DOMContentLoaded', () => {
    registerNavbarBurger();

    generateTableOfContent();
});
