window.languageStorage = {
    get: () => localStorage.getItem('app-language'),
    set: (lang) => localStorage.setItem('app-language', lang)
};