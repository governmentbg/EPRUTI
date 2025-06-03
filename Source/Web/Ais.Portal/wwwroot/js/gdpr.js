//GDPR-Cookie policy warning by IvuWorks
//Fixed by PramtarovTech
function waitForResources(callback) {
    if (window.isLoaded == true) {
        callback();
    } else {
        setTimeout(function () {
            waitForResources(callback);
        }, 50)
    }
}

$(function () {
    waitForResources(function () {
        var TRACK = false;
        if (globalVariables.cookiesUrl && globalVariables.termsUrl) {
            gdpr_htmlstring = '<div id="gdpr_wrapper">'
                + '<div class="center">' +
                '<div><img src="/images/cookie.png" alt="gdrp"></div><p class="gdpr_rm">'
                + window.getResource("GDPRBaseText") +
                '<a href="' + globalVariables.cookiesUrl + ` "style="color:#fff; text-decoration: underline;"><em>${window.getResource("GDPRCookiesText")}</em></a> `
                + `${window.getResource("And")} `
                + '<a href="' + globalVariables.termsUrl + ` "style="color:#fff;text-decoration: underline;">${window.getResource("GDPRTermsText")}<em></em></a> `
                + '</p><div class="gdpr_bttns">'
                + '<a id="gdpr_agree" href="#" class="bttn main" >'
                + window.getResource("GDPRAgreeButton")
                + '</a>'
                + '</div></div>';
        }

        function setCookie(cname, cvalue, exdays) {
            var d = new Date();
            d.setTime(d.getTime() + (exdays * 24 * 60 * 60 * 1000));
            var expires = "expires=" + d;
            document.cookie = cname + "=" + cvalue + "; " + expires + "; path=/";
        }
        function getCookie(cname) {
            var name = cname + "=";
            var ca = document.cookie.split(';');
            for (var i = 0; i < ca.length; i++) {
                var c = ca[i];
                while (c.charAt(0) == ' ') c = c.substring(1);
                if (c.indexOf(name) == 0) return c.substring(name.length, c.length);
            }
            return "";
        }

        // Cookies consent
        $('body').on('click', '#gdpr_agree,.gdpr_agree,.privacybtn1,.success.privacybtn', function (e) {
            e.preventDefault();
            setCookie('cookiesgdpr', 'true', 365);
            $('#gdpr_wrapper').fadeOut(200).remove();
        });

        if (getCookie('cookiesgdpr')) {
            //some action taken
            if (getCookie('cookiesgdpr') == "false") {
                //refused, will show in an hour, will not track
                TRACK = false;
                //$('iframe').remove();  
                //use condition to remove GA/FB codes
            } else {
                //accepted and valid for 1 year
                TRACK = true;
            }
        } else {
            //not accepted, show popup
            $('header').before(gdpr_htmlstring);
            TRACK = false;
        }
    })
});
