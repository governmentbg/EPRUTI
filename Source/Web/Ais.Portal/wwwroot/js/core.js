/// <reference path="../../scripts/utilities/resources.ts" />

$("#respmenu").on("click", function (e) {
    $("body").toggleClass("openedmenu");
});

//SLIDE TO ELEMENT class and function
function goToThis(a) {
    var b = 0;
    if (a !== undefined && a !== "" && a != "#") {
        b = $(a).offset().top;
    } else $("html,body").animate({ scrollTop: b }, 600);
}

$("body").on("click", ".gotothis", function (e) {
    e.preventDefault();
    var v = $(this).attr("href");
    if (v === undefined || v == "") {
        v = $(this).data("gotothis");
    }
    if (v == "javascript:;" || v == "false") {
        v = false;
    }
    goToThis(v);
});

if ($(".follow").length)
    $(document).mousemove(function (e) {
        $(".follow").css({ top: e.pageY + 30, left: e.pageX + 30 });
    });

if ($(".bindselect").length)
    $(".bindselect + select").on("change", function (e) {
        e.preventDefault();
        $(this).prev().html($(this).val().toString());
    });
//detect TOUCHscreen
//('ontouchstart' in window || navigator.maxTouchPoints)? $('body').addClass('isTouch'):$('body').addClass('noTouch');
$("body").addClass(
    "touch_" +
    (Boolean("ontouchstart" in window || navigator.maxTouchPoints) + "")
);

$(".js-show-password").on("click", function () {
    var inputField = $(this).siblings('.password')[0]
    if (inputField.type == "password") {
        inputField.type = 'text';
        $(this).removeClass('k-i-eye').addClass('k-i-eye-slash')
    } else {
        inputField.type = "password";
        $(this).removeClass('k-i-eye-slash').addClass('k-i-eye')
    }
});

//FIXATE MENU
var fixat = $("header nav").offset().top;
$(window).scroll(function () {
    if ($(this).scrollTop() > fixat && !$("body").hasClass("fixed")) {
        $("body").addClass("fixed");
    } else if ($(this).scrollTop() <= fixat && $("body").hasClass("fixed")) {
        $("body").removeClass("fixed");
    }
    var asideHeight = $(".content-pre-wrap ul").height();
    if ($(window).width() > 768) {
        if (
            $(this).scrollTop() > 200 &&
            asideHeight < $(".content-wrap").height() &&
            asideHeight < $(window).height() &&
            asideHeight + $(this).scrollTop() - 107 < $(".content-pre-wrap").height()
        ) {
            $("aside").css({
                top: $(this).scrollTop() - 170 + "px",
            });
        } else if ($(this).scrollTop() < 200) {
            $("aside").attr("style", "");
        }
    }
});

$("#news .itemlist")
    .addClass("owl-carousel")
    .owlCarousel({
        items: 4,
        responsive: {
            1080: { items: 4 },
            768: { items: 3 },
            480: { items: 2 },
            0: { items: 1 },
        },
        dots: true,
        nav: false,
        margin: 20,
        dotsEach: 1,
    });

$("#events .itemlist")
    .addClass("owl-carousel")
    .owlCarousel({
        items: 2,
        responsive: {
            1200: { items: 2 },
            1024: { items: 1 },
            600: { items: 2 },
            0: { items: 1 },
        },
        dots: true,
        nav: false,
        margin: 20,
        dotsEach: 1,
        loop: true,
        autoplay: true,
        autoplayTimeout: 3000,
        autoplayHoverPause: true
    });

$("#services .itemlist")
    .addClass("owl-carousel")
    .owlCarousel({
        items: 4,
        responsive: {
            1080: { items: 4 },
            768: { items: 3 },
            480: { items: 2 },
            0: { items: 1 },
        },
        dots: true,
        nav: false,
        margin: 20,
        dotsEach: 1,
    });

$(".js-show-email").on("click", function (e) {
    e.preventDefault();
    $(this)
        .html($(this).data("mail"))
        .attr("href", "mailto:" + $(this).data("mail"))
        .off();
});

// visualy impaired
$(".js-trigger-blind").on("click", function (e) {
    e.preventDefault();
    if (localStorage.getItem("textmode") != "true") {
        $('link[rel="stylesheet"]').prop("disabled", true);
        $(this).addClass("turn-off");
        let newTitle = resources.getResource("ForBlindedOff");
        $(this).html(`<a title='${newTitle}' style='color:red; cursor:pointer'>${newTitle}</a>`);
        localStorage.setItem("textmode", "true");
        // if ($(this).find(".skip").length === 0) {
        //     $(this).after('<a href="#content" class="skip"> Пропусни хедърна част </a>');
        // }
    } else {
        $('link[rel="stylesheet"]').prop("disabled", false);
        localStorage.removeItem("textmode");
        let newTitle = resources.getResource("ForBlinded");
        $(this).html(`<a title='${newTitle}' class='pointer text-link js-trigger-blind transparent'>${newTitle}</a>`);
        // $(this).removeClass("turn-off");
        // $(".skip").remove();
    }
});

if (localStorage.getItem("textmode")) {
    $('link[rel="stylesheet"]').prop("disabled", true);
    $(this).html(`<a title='${newTitle}' style='color:red; cursor:pointer'>${newTitle}</a>`);
    if ($(".skip").length === 0) {
        //$("#textmode").after('<a href="#content" class="skip">РџСЂРѕРїСѓСЃРЅРё РѕСЃРЅРѕРІРЅРѕ РјРµРЅСЋ</a>');
    }
}

//end visual

//-----------   A+A-A FONT CONTROL  ---------------------------//
//set cookie
function setCookie(cname, cvalue, exdays) {
    var d = new Date();
    d.setTime(d.getTime() + exdays * 24 * 60 * 60 * 1000);
    var expires = "expires=" + d.toUTCString();
    document.cookie = cname + "=" + cvalue + "; " + expires + "; path=/";
}
//get cookie
function getCookie(cname) {
    var name = cname + "=";
    var ca = document.cookie.split(";");
    for (var i = 0; i < ca.length; i++) {
        var c = ca[i];
        while (c.charAt(0) == " ") c = c.substring(1);
        if (c.indexOf(name) == 0) return c.substring(name.length, c.length);
    }
    return "";
}
//check for cookie, if there is one, set body
var check = localStorage.getItem("text");
if (check != "") {
    $("body").addClass(check);
}
//list of all classes
var classes = [
    "t05",
    "t06",
    "t07",
    "t08",
    "t09",
    "t00",
    "t11",
    "t12",
    "t13",
    "t14",
    "t15",
    "t16",
    "t18",
    "t20",
    "t22",
];
var classes = ["t10", "t00", "t16"];
//control over website
$(".js-trigger-text").on("click", function (e) {
    e.preventDefault();
    var direction = $(this).data("direction") == "up" ? true : false, //up and down, true and false
        flag = false,
        getindex = "",
        currentClasses = $("body").attr("class").split(" ");
    for (var i = 0; i < classes.length; i++) {
        if ($.inArray(classes[i], currentClasses) > -1) {
            getIndex = $.inArray(classes[i], currentClasses);
            if (direction) {
                getClass = classes[i + 1];
            } else {
                getClass = classes[i - 1];
            }
            flag = false;
            break;
        } else {
            flag = true;
        }
    }
    if (flag) {
        if (direction) {
            $("body").addClass("t16");
            getClass = "t16";
        } else {
            $("body").addClass("t09");
            getClass = "t09";
        }
    } else {
        currentClasses.splice(getIndex, 1);
        currentClasses.push(getClass);
        $("body").attr("class", currentClasses.join(" "));
    }
    //remember the choice
    localStorage.setItem("text", getClass);
});
//reset cookie and font
$("#reset").on("click", function (e) {
    e.preventDefault();
    $("body").removeClass(
        "t05 t06 t07 t08 t09 t00 t11 t12 t13 t14 t15 t16 t18 t20 t22"
    );
    localStorage.setItem("text", "");
});
//-----------   END A+A-A   ---------------------------//

$(".js-togggle-application-mode").on("click", function (e) {
    e.preventDefault();
    $("body").toggleClass("application");
    var masonryLayout = $("body").find('.steps-body.js-masonry-layout')
    var mode = $(this).find("span")

    //fix step-box resizing on mode change
    if (masonryLayout) {
        masonryLayout.removeClass("js-masonry-layout").addClass("js-masonry-layout")
    }

    if ($("body").hasClass("application")) {
        mode.text(resources.getResource("ChangeApplicationMode"))
    } else {
        mode.text(resources.getResource("ApplicationMode"))
    }
});

$(document).ready(function () {
    var selector = ".responsivenav nav, .steps-results-wrap";
    var scrollBarElements = $(selector);
    if (scrollBarElements.length > 0) {
        new PerfectScrollbar(".responsivenav nav, .steps-results-wrap");
    }

    //prevent auto scrolling to top
    var masonryLayout = $(".steps-body.js-masonry-layout")[0];
    if (masonryLayout) {
        let resizeObserver = new ResizeObserver(() => {
            var masonryHeight = masonryLayout.style.height;
            $(".steps-body.js-masonry-layout").css('min-height', masonryHeight);
        });
        resizeObserver.observe(masonryLayout);
    }
});

if (window.innerWidth > 1240) {
    $(".js-masonry-layout").one("click", () => {
        let currentHeight = $('.js-masonry-layout').height();
        $('.js-masonry-layout').css("min-height", currentHeight)
    });
}

/*
$('#submit-search').on('click',function(e){
    $('.mc-results').slideDown(200);
    
});
*/