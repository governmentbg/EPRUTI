//GENERAL ERROR POPUP
function genpop(a, b, c) {
    //content, customclass, title
    $('#genpop').remove();
    if (typeof (event) !== 'undefined')
        event.preventDefault();
    if (c != undefined && c != '') {
        switch (b) {
            case 'success': c = 'Success'; break;
            case 'warning': c = 'Warning'; break;
            case 'note': c = 'Note'; break;
            case 'error': c = 'Error'; break;
            default: break;
        }
    }
    var c = '<div id="genpop" class="' + b + '"><div class="genbody"><div class="icon"></div><div>' + ((c == '') ? '' : ('<h6>' + c + '</h6>'))
        + '<div class="p">' + a + '</div><a href="#" class="_close">x</a></div></div></div>';
    $('body').append(c);
    $('input,textarea').blur();
}

$(document).keyup(function (e) {
    if (e.keyCode == 27) {
        $('#genpop ._close').click();   // esc
        $('#respmenu').prop('checked', false);
        $('body').removeClass('openedmenu openedsearch');
    }
});

$('#respmenu').on('click', function (e) {
    $('body').toggleClass('openedmenu');
});

if ($('.responsivenav nav').length > 0) {
    const ps = new PerfectScrollbar('.responsivenav nav');
}

$('body').on('click', '#genpop ._close', function (e) {
    e.preventDefault();
    $('#genpop').fadeOut(300);
    setTimeout(function () {
        $('#genpop').remove();
    }, 300);
}); //$('body').removeClass('overflow-hidden')



//SLIDE TO ELEMENT class and function
function goToThis(a) {
    //console.log('a is '+a);
    var b = 0;
    if (a !== undefined && a !== '' && a != '#') {
        b = $(a).offset().top;
    } else
        $('html,body').animate({ scrollTop: b }, 600);
}

$('body').on('click', '.gotothis', function (e) {
    e.preventDefault();
    var v = $(this).attr('href');
    if (v === undefined || v == '') {
        v = $(this).data('gotothis');
    }
    if (v == 'javascript:;' || v == 'false') { v = false; }
    goToThis(v);
});


//detect TOUCHscreen 
//('ontouchstart' in window || navigator.maxTouchPoints)? $('body').addClass('isTouch'):$('body').addClass('noTouch');
$('body').addClass('touch_' + (Boolean('ontouchstart' in window || navigator.maxTouchPoints) + ''));


//FIXATE MENU
if ($('header .header-bottom-submenu').length > 0) {
    var fixat = $('header .header-bottom-submenu').offset().top;
}
$(window).scroll(function () {
    //console.log($(this).scrollTop());
    if ($(this).scrollTop() > fixat && !$('body').hasClass('fixed')) {
        $('body').addClass('fixed');
    } else if ($(this).scrollTop() <= fixat && $('body').hasClass('fixed')) {
        $('body').removeClass('fixed');
    }
    var asideHeight = $('.content-pre-wrap ul').height();
    //console.log(asideHeight);
    if ($(window).width() > 768) {
        if ($(this).scrollTop() > 200
            && asideHeight < $('.content-wrap').height()
            && asideHeight < $(window).height()
            && (asideHeight + $(this).scrollTop() - 107) < $('.content-pre-wrap').height()) {
            $('aside').css({
                'top': ($(this).scrollTop() - 170) + 'px'
            });
        } else if ($(this).scrollTop() < 200) {
            $('aside').attr('style', '');
        }
    }

    checkToolbar()
});

$('.toggle-map-controls').on('click', function (e) {
    e.preventDefault();
    $('.map-control-wrap').stop().toggleClass('collapsed-controls');
});

$('.js-toggle-center').on('click', function (e) {
    e.preventDefault();
    $(this).parents('.center').toggleClass('fullwidth');
});

$('.js-toggle-menu').on('click', function (e) {
    e.preventDefault();
    $(this).parents('.userlist-wrap').find('.userlist-task-list').stop().slideToggle(200);
});

$('.js-toggle-list').on('click', function (e) {
    e.preventDefault();
    $(this).parents('.userlist-wrap').find('.userlist-checkboxes').stop().slideToggle(200);
});

var checkFont = localStorage.getItem("text");
if (checkFont != "") {
    $("body").addClass(checkFont);
}

$(".js-trigger-text").on("click", function (e) {
    e.preventDefault();
    $("body").addClass("t16");
    localStorage.setItem("text", "t16");
    checkMenu();
});

$("#resetFont").on("click", function (e) {
    e.preventDefault();
    $("body").removeClass("t16");
    localStorage.setItem("text", "");
    checkMenu();
});

//NAVIAGTION
//add item to threedots
function addThreeDots() {
    var item = $("nav li.threedots").prev();
    itemp = item.width() + 30; //+margins + threshold
    //console.log('item hidden from menu');
    if (item.length)
        $("nav li.threedots > ul").prepend(item);
    if ($("#threedots li").length != 0)
        $("#threedots").removeClass("hidden").parent().removeClass('hidden');
}

function removeThreeDots() {
    $("#threedots").addClass("hidden").parent().addClass('hidden');
}

function addMenuItem(viewportWidth, itemwidth) {
    //console.log('item added to menu');
    var item = $("#threedots li").eq(0);
    $("nav li.threedots").before(item);
    itemp = item.width() + 30; //+margins + threshold
    if ($("#threedots li").length === 0)
        $("#threedots").addClass("hidden");
}

var itemp = $("nav li.threedots").prev().width() + 24; //item temporary width + margins

function checkMenu() {
    var itemwidth = $(".header-bottom nav > ul").innerWidth() +
        $(".header-bottom nav > .right").innerWidth() + 90;
    var isFontChange = $("body").hasClass("t16");

    var viewportWidth = window.innerWidth > `1480 + ${isFontChange ? 30 : 0}` ? `1480 + ${isFontChange ? 30 : 0}` : window.innerWidth;
    if (viewportWidth - itemwidth < 0) {
        if ($("nav li.threedots").prev().length) {
            addThreeDots();
            checkMenu();
        }
    }
    else if ((viewportWidth - itemwidth) > itemp) {
        addMenuItem(viewportWidth, itemwidth);
        checkMenu();
    }
}

function checkToolbar() {
    try {
        var dataTableElement = $('#datatable');
        if (!dataTableElement) {
            return;
        }

        var toolbar = dataTableElement.find('.k-grid-toolbar.k-toolbar');
        if (!toolbar) {
            return;
        }

        var table = dataTableElement.offset().top;
        if ($(this).scrollTop() >= table) {
            toolbar.addClass('floading-toolbar');
            if ($('header .header-bottom-submenu .center').length > 0 && $(this).innerWidth() > 480) {
                var submenu = $('header .header-bottom-submenu').height();
                toolbar.css("top", submenu);
            } else { toolbar.css("top", "0") }
        }
        else {
            toolbar.removeClass('floading-toolbar').css("top", "0")
        }
    } catch {
    }
}


$(window).resize(function () {
    var isFontChange = $("body").hasClass("t16");

    if (window.innerWidth > 746)
        checkMenu();

    if (window.innerWidth > `1480 + ${isFontChange ? 30 : 0}`)
        removeThreeDots();

    checkToolbar();
});

$("nav li.threedots").on("click",
    function (e) {
        if ($(e.target).parent().hasClass("threedots")) {
            e.preventDefault();
            $("body").toggleClass("openedmenu");
        }
    }
);

$("nav li a .icon").on("click",
    function (e) {
        if (e.target.tagName.toLowerCase() == "svg" || $(e.target).parents("svg").length) {
            e.stopImmediatePropagation();
            e.stopPropagation();
            e.preventDefault();
            $(this).parents("li").eq(0).toggleClass("expanded");
        }
    }
);

setTimeout(function () {
    checkMenu();
},
    10);

////$('.header-bottom .right > a').on('click', function (e) {
////    e.preventDefault();
////    $(this).parent().toggleClass('opened');
////    $("body").toggleClass('openedprofilemenu');
////});
//END NAVIAGTION