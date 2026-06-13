mergeInto(LibraryManager.library, {
    Project_GetAutoLanguage: function () {
        var language = "";

        try {
            if (typeof window !== "undefined") {
                if (window.ysdk &&
                    window.ysdk.environment &&
                    window.ysdk.environment.i18n &&
                    typeof window.ysdk.environment.i18n.lang === "string" &&
                    window.ysdk.environment.i18n.lang.length > 0) {
                    language = window.ysdk.environment.i18n.lang;
                } else if (typeof ysdk !== "undefined" &&
                    ysdk &&
                    ysdk.environment &&
                    ysdk.environment.i18n &&
                    typeof ysdk.environment.i18n.lang === "string" &&
                    ysdk.environment.i18n.lang.length > 0) {
                    language = ysdk.environment.i18n.lang;
                } else if (typeof window.ProjectDetectedLanguage === "string" && window.ProjectDetectedLanguage.length > 0) {
                    language = window.ProjectDetectedLanguage;
                } else if (window.location && typeof window.location.search === "string") {
                    var match = window.location.search.match(/[?&]lang=([^&]+)/i);
                    if (match && match.length > 1) {
                        language = decodeURIComponent(match[1]);
                    }
                }

                if ((!language || language.length === 0) && typeof navigator !== "undefined" && navigator.language) {
                    language = navigator.language;
                }
            }
        } catch (error) {
            language = "";
        }

        var length = lengthBytesUTF8(language) + 1;
        var buffer = _malloc(length);
        stringToUTF8(language, buffer, length);
        return buffer;
    }
});
