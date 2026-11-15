(() => {
    const menu = document.querySelector('[data-menu-toggle]');
    const sidebar = document.getElementById('portal-sidebar');
    const mobile = window.matchMedia('(max-width: 760px)');
    const closeMenu = () => {
        document.body.classList.remove('sidebar-open');
        menu?.setAttribute('aria-expanded', 'false');
        if (sidebar && mobile.matches) sidebar.inert = true;
    };
    const synchronizeMenu = () => {
        closeMenu();
        if (sidebar) sidebar.inert = mobile.matches;
    };
    synchronizeMenu();
    mobile.addEventListener('change', synchronizeMenu);
    menu?.addEventListener('click', () => {
        if (document.body.classList.contains('sidebar-open')) {
            closeMenu();
        } else {
            document.body.classList.add('sidebar-open');
            menu.setAttribute('aria-expanded', 'true');
            sidebar.inert = false;
            sidebar.querySelector('a')?.focus();
        }
    });
    document.querySelector('[data-menu-close]')?.addEventListener('click', () => {
        closeMenu();
        menu?.focus();
    });
    sidebar?.querySelectorAll('a').forEach(link => link.addEventListener('click', closeMenu));
    sidebar?.addEventListener('keydown', event => {
        if (!mobile.matches || !document.body.classList.contains('sidebar-open') || event.key !== 'Tab') return;
        const links = Array.from(sidebar.querySelectorAll('a, button')).filter(item => !item.disabled);
        if (event.shiftKey && document.activeElement === links[0]) {
            event.preventDefault(); links.at(-1)?.focus();
        } else if (!event.shiftKey && document.activeElement === links.at(-1)) {
            event.preventDefault(); links[0]?.focus();
        }
    });
    const commands = document.getElementById('workspace-commands');
    const commandInput = document.querySelector('[data-command-input]');
    const commandItems = Array.from(document.querySelectorAll('[data-command-item]'));
    const filterCommands = () => {
        const term = commandInput.value.trim().toLocaleLowerCase();
        commandItems.forEach(item => { item.hidden = !item.textContent.toLocaleLowerCase().includes(term); });
        const empty = document.querySelector('[data-command-empty]');
        if (empty) empty.hidden = commandItems.some(item => !item.hidden);
    };
    const openCommands = () => {
        if (!commands || commands.open) return;
        commandInput.value = '';
        filterCommands();
        commands.showModal();
        commandInput.focus();
    };
    document.querySelector('[data-command-open]')?.addEventListener('click', openCommands);
    document.querySelector('[data-command-close]')?.addEventListener('click', () => commands.close());
    commandInput?.addEventListener('input', filterCommands);
    commandInput?.addEventListener('keydown', event => {
        if (event.key === 'Enter') {
            const first = commandItems.find(item => !item.hidden);
            if (first) { event.preventDefault(); first.click(); }
        }
    });
    commands?.addEventListener('click', event => { if (event.target === commands) commands.close(); });
    document.addEventListener('keydown', event => {
        if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
            event.preventDefault(); openCommands();
        }
        if (event.key === 'Escape' && document.body.classList.contains('sidebar-open')) {
            closeMenu(); menu?.focus();
        }
    });
    document.querySelectorAll('[data-dismiss-alert]').forEach(button => button.addEventListener('click', () => button.closest('.portal-alert').remove()));
    document.querySelectorAll('[data-current-date]').forEach(element => {
        element.textContent = new Intl.DateTimeFormat('en-GB', {day:'numeric', month:'short', year:'numeric'}).format(new Date());
    });
})();
