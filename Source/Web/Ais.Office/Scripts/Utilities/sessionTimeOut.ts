import { createKendoDialog, requestOptionalUrl } from 'scripts/Utilities/core';

export default passedOptions => {
    const defaults = {
        appendTimestamp: true,
        keepAliveMethod: "POST",
        keepAliveUrl: "/keep-alive",
        message: "Your session is about to expire.",
        stayConnectedBtnText: "Stay connected",
        cancelBtnText: "Cancel",
        timeOutAfter: 1200000,
        timeOutUrl: "/timed-out",
        titleText: "Session Timeout",
        warnAfter: 900000
    };

    const options = Object.assign(defaults, passedOptions);

    let warnTimer;

    const warn = () => {
        const timeOutInterval = options.timeOutAfter - options.warnAfter;
        let timeOutTimer = setTimeout(timeOut, timeOutInterval - 1000);
        clearTimeout(warnTimer);

        const actions = [
            {
                text: options.stayConnectedBtnText,
                action: () => {
                    const url = options.appendTimestamp
                        ? `${options.keepAliveUrl}?time=${Date.now()}`
                        : options.keepAliveUrl;

                    requestOptionalUrl(
                        url,
                        {
                            type: options.keepAliveMethod,
                            success: () => {
                                warnTimer = setTimeout(warn, options.warnAfter);
                                clearTimeout(timeOutTimer);
                            }
                        }
                    );

                    return true;
                },
                primary: true
            },
            {
                text: options.cancelBtnText
            }
        ];

        const html = `${options.message}<br\><div class='session-countdown-js'></div>`
        createKendoDialog(
            {
                title: options.titleText,
                content: html,
                visible: true,
                modal: true,
                actions: actions
            });
        startTimer(timeOutInterval / 1000, $(".session-countdown-js:last"));
    };

    const timeOut = () => {
        window.location = options.timeOutUrl;
    };

    const startTimer = (duration: number, display: JQuery) => {
        var timer = duration, minutes, seconds;
        let writeTime = () => {
                minutes = parseInt((timer / 60).toString(), 10)
                seconds = parseInt((timer % 60).toString(), 10);

                minutes = minutes < 10 ? "0" + minutes : minutes;
                seconds = seconds < 10 ? "0" + seconds : seconds;

                display.html(minutes + ":" + seconds);

                if (--timer < 0) {
                    timer = duration;
                }
            };
        writeTime();
        setInterval(writeTime, 1000);
    }

    warnTimer = setTimeout(warn, options.warnAfter);
};