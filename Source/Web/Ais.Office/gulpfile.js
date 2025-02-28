import gulp from 'gulp';
import concat from "gulp-concat";
import cssmin from "gulp-cssmin";
import sourcemaps from "gulp-sourcemaps";
import uglify from "gulp-uglify";
import merge from "merge-stream";
import { deleteAsync } from "del";
import dartSass from 'sass';
import gulpSass from 'gulp-sass';
const sass = gulpSass(dartSass);
import noop from "gulp-noop";

const regex = {
    css: /\.css$/,
    js: /\.js$/
};

const isDevMode = process.env.NODE_ENV === 'development' ? 'development' : 'production';

const systemStyle = () => {
    const pathModule = path.resolve(`./appsettings.${isDevMode ? "Development" : "AgccProd"}.json`);
    const data = fs.readFileSync(pathModule, 'utf8');
    const appSettings = JSON.parse(data);
    return appSettings.SystemStyle.IsNullOrEmpty() ? null : "-" + appSettings.SystemStyle
}

const bundleConfig = [
    {
        "outputFileName": "wwwroot/bundles/common.min.css",
        "inputFiles": [
            "wwwroot/css/ais.css",
            "wwwroot/css/perfect-scrollbar.css",
            "wwwroot/css/owl.carousel.min.css",
            "wwwroot/css/magnific-popup.css",
            `wwwroot/scss/style${systemStyle}.css`,
            `wwwroot/css/style.custom${systemStyle}.css`,
            "wwwroot/css/cropper.css",
        ]
    },
    {
        "outputFileName": "wwwroot/bundles/ol.min.css",
        "inputFiles": [
            "node_modules/ol/ol.css"
        ]
    },
    {
        "outputFileName": "wwwroot/bundles/jquery.min.js",
        "inputFiles": [
            "node_modules/jquery/dist/jquery.min.js",
            "node_modules/jquery-validation/dist/jquery.validate.min.js",
            "node_modules/jquery-validation-unobtrusive/dist/jquery.validate.unobtrusive.min.js",
            "node_modules/jquery-ajax-unobtrusive/dist/jquery.unobtrusive-ajax.min.js",
            "node_modules/jquery.dirtyforms/jquery.dirtyforms.min.js",
            "node_modules/jQuery.print/jQuery.print.js"
        ]
    },
    {
        "outputFileName": "wwwroot/bundles/common.min.js",
        "inputFiles": [
            "node_modules/@microsoft/signalr/dist/browser/signalr.min.js",
            "wwwroot/js/kendo-ui-license.js",
            "wwwroot/js/jquery.magnific-popup.min.js",
            "wwwroot/js/owl.carousel.min.js",
            "wwwroot/js/perfect-scrollbar.min.js",
            "wwwroot/js/modernizr-custom.js",
            "wwwroot/js/masonry.min.js",
            "wwwroot/js/cachesvg.js",
            "wwwroot/js/core.js",
            "wwwroot/bundles/resources.min.js",
            "wwwroot/bundles/notification.min.js",
            "wwwroot/bundles/core.min.js",
            "wwwroot/bundles/searchTable.min.js",
            "wwwroot/bundles/signalr.min.js",
            "wwwroot/bundles/sessionTimeOut.min.js",
            "wwwroot/js/cropper.min.js",
            "wwwroot/js/jquery-cropper.min.js",
            "wwwroot/bundles/cart.min.js",
            "wwwroot/js/scs/scs.js",
            "wwwroot/js/scs/scs.helpers.js",
        ]
    },
    {
        "outputFileName": "wwwroot/bundles/applicationbundle.min.js",
        "inputFiles": [
            "wwwroot/bundles/applicationInfo.min.js",
            "wwwroot/bundles/application.min.js",
        ]
    },
    {
        "outputFileName": "wwwroot/bundles/outapplicationbundle.min.js",
        "inputFiles": [
            "wwwroot/bundles/applicationInfo.min.js",
            "wwwroot/bundles/application.min.js",
            "wwwroot/bundles/outapplication.min.js",
        ]
    },
    {
        "outputFileName": "wwwroot/bundles/kendo-custom-messages-bg.min.js",
        "inputFiles": [
            "wwwroot/js/kendo-custom-messages-bg.js",
        ]
    },
]

const sassConfig =
    [
        {
            "input": "wwwroot/scss/style.scss",
            "output": "wwwroot/scss/style.css"
        }
    ]

function cleanStyles() {
    var files = sassConfig.map(function (bundle) {
        return bundle.output;
    });

    return deleteAsync(files);
}

function buildStyles() {
    var tasks = sassConfig.map(function (style) {
        return gulp.src(style.input, { base: "." })
            .pipe(sass().on('error', sass.logError))
            .pipe(gulp.dest('.'));
    });

    return merge(tasks);
};

function getBundles(regexPattern) {
    return bundleConfig.filter(function (bundle) {
        return regexPattern.test(bundle.outputFileName);
    });
}

function cleanBundles() {
    var files = bundleConfig.map(function (bundle) {
        return bundle.outputFileName;
    });

    return deleteAsync(files);
}

function cssBundles() {
    var tasks = getBundles(regex.css).map(function (bundle) {
        return gulp.src(bundle.inputFiles, { base: "." })
            .pipe(concat(bundle.outputFileName))
            .pipe(gulp.dest("."));
    });

    return merge(tasks);
}

function cssMin() {
    var tasks = getBundles(regex.css).map(function (bundle) {
        return gulp.src(bundle.outputFileName, { base: "." })
            .pipe(isDevMode ? noop() : cssmin())
            .pipe(gulp.dest("."));
    });

    return merge(tasks);
}

function jsBundles() {
    var tasks = getBundles(regex.js).map(function (bundle) {
        return gulp.src(bundle.inputFiles, { base: "." })
            .pipe(sourcemaps.init({ loadMaps: true }))
            .pipe(concat(bundle.outputFileName))
            .pipe(sourcemaps.write())
            .pipe(gulp.dest("."));
    });

    return merge(tasks);
}

function jsUglify() {
    var tasks = getBundles(regex.js).map(function (bundle) {
        return gulp.src(bundle.outputFileName, { base: "." })
            .pipe(isDevMode ? noop() : uglify())
            .pipe(gulp.dest("."));
    });

    return merge(tasks);
}

export function watch(cb) {
    sassConfig.forEach(function (bundle) {
        gulp.watch(bundle.input, gulp.series(buildStyles, cssBundles, cssMin));
    });

    getBundles(regex.css).forEach(function (bundle) {
        gulp.watch(bundle.inputFiles, gulp.series(cssBundles, cssMin));
    });

    getBundles(regex.js).forEach(function (bundle) {
        gulp.watch(bundle.inputFiles, gulp.series(jsBundles, jsUglify));
    });

    cb();
}

export default gulp.series(
    gulp.parallel(cleanStyles, cleanBundles),
    buildStyles,
    gulp.parallel(cssBundles, jsBundles),
    gulp.parallel(cssMin, jsUglify)
);

export const clean = gulp.parallel(cleanStyles, cleanBundles);