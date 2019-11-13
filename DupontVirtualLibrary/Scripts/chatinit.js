$(document).ready(function () {
    const params = BotChat.queryParams(location.search);
    var parentUrl = window.parent.location;
    var parentUrlTest = document.referrer;
    const user = {
        id: params['koid'] || 'userid' + Math.floor((Math.random() * 10000000) + 1),
        name: (params['firstName'] + '-' + params['country']) || 'UnknownUser'
    };
    const bot = {
        id: params['botid'] || 'wilsonMB-Dev',
        name: params['botname'] || 'wilson'
    };
    window['botchatDebug'] = params['debug'] && params['debug'] === 'true';

    //var saraMBDirectLine = "xwe6NYTbL4U.cwA.ENc.VNxds6WvgnJMJD0O5MFSA9CWdRUygNKxrs1BWRnNMkI";
    //var saraMBDevDirectLine = "XoGzfIr9HSM.cwA.iFs.tBMLLlx0rn0oeMn67fBQsjV_Y3bwDtLTd-Ekk1wsetY";
    //var saraMB = "fUCY1Cb6QwI.cwA.vvE.QZ6Rfy5HqE_O5sYJpnwE6z6SSOtnhj1Mpd0c5Zmwe-U";
    //var saraMBTCCCDevDirectLine = "jjs3esd7bEI.cwA.GLk.KzJdcj-tiR5zJeY8pu7EqbXMa88w1ovVvb7z3GUVZUo";
    //var currentDL = saraMBDirectLine;
    
	var WilsonDevDirectLine = "jZE0Qw6RhlA._TY1YQq2GUQxc3NQFgi97CHH_MXjG2eSNzD1ZpaVduk";
    //var saraMBDevDirectLine = "vOnNtxmsUAk.cwA.nEE.4vGokngJdJQjFF9SxlqGi39DzXD63PzBLnAaU2L1v3U";
    //var saraMB = "vOnNtxmsUAk.cwA.nEE.4vGokngJdJQjFF9SxlqGi39DzXD63PzBLnAaU2L1v3U";
    //var saraMBTCCCDevDirectLine = "vOnNtxmsUAk.cwA.nEE.4vGokngJdJQjFF9SxlqGi39DzXD63PzBLnAaU2L1v3U";
    var currentDL = WilsonDevDirectLine;
	


    var botConnection = new BotChat.DirectLine({
        secret: currentDL,
        conversationId: params["c"],
        domain: params["domain"],
        webSocket: params["webSocket"] && params["webSocket"] === "true" // defaults to true
    });

    BotChat.App({
        bot: bot,
        botConnection: botConnection,
        user: user
    }, document.getElementById('BotChatGoesHere'));

    // handle backchannel events
    botConnection.activity$
        .filter(function (activity) { return activity.type === "event" && activity.name === "setToken"; })
        .subscribe(function (activity) { setToken(activity.value); });

    // send a start event to kick off the conversation
    if (typeof params['start'] === "undefined")
        params['start'] = "";
    botConnection
        .postActivity({ type: "event", value: params['start'], from: { id: user.id }, name: "start" })
        .subscribe(function (id) { console.log("start conv. Conv ID:" + id); });

    // add this event handler to any expandable item when they are dynamically created
    // in the DOM
    $(document).on('click', '.expandable', function () {
        // get the closest parent message holder and then the next sibling to see if it
        // got pushed off the screen.
        var p = $(this).closest(".wc-message-wrapper").next();
        var height = document.getElementsByClassName("wc-message-groups")[0].clientHeight;

        var rect = p[0].getBoundingClientRect();
        if (rect.bottom > height) {
            p[0].scrollIntoView(false);
            setTimeout(function () {
                document.getElementsByClassName("wc-message-groups")[0].scrollTop =
                    document.getElementsByClassName("wc-message-groups")[0].scrollTop + 18;
            });
        }
    });
});