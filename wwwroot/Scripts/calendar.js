var _months = ["January","February","March","April","May","June","July","August","September","October","November","December"];
var _monthsShort = ["Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec"];
var _daysShort = ["Mon","Tue","Wed","Thu","Fri","Sat","Sun"]; // JS getDay(): 0=Sun→index 6, 1=Mon→index 0, …, 6=Sat→index 5

function pad2(n) { return n < 10 ? '0' + n : '' + n; }

// Parse ISO date string manually — old WebKit (wkhtmltopdf 0.12.4) can't reliably parse
// ISO 8601 T-format datetime strings via new Date(). Ignores timezone, treats as local.
function parseISODate(isoStr) {
    if (!isoStr) return new Date(0);
    var m = /^(\d{4})-(\d{2})-(\d{2})(?:[T ](\d{2}):(\d{2}))?/.exec(isoStr);
    if (!m) return new Date(0);
    return new Date(parseInt(m[1]), parseInt(m[2]) - 1, parseInt(m[3]),
                    m[4] ? parseInt(m[4]) : 0, m[5] ? parseInt(m[5]) : 0);
}

function dateToYYYYLLDD(d) {
    return '' + d.getFullYear() + pad2(d.getMonth() + 1) + pad2(d.getDate());
}

// Map JS getDay() (0=Sun…6=Sat) to _daysShort index (0=Mon…6=Sun)
function jsDayToShort(jsDay) {
    return _daysShort[jsDay === 0 ? 6 : jsDay - 1];
}

$('#loading').hide();
var $content = $('#content').show();

$('#eventModal').on('show.bs.modal', function (event) {
    var button = $(event.relatedTarget);
    var summary = button.data('summary');
    var description = button.data('description');
    var startdate = parseISODate(button.data("start-date"));
    var endDate = parseISODate(button.data('end-date'));
    var modal = $(this);
    modal.find('#eventModalTitle').text(summary);
    modal.find('#eventModalStartDate').text(pad2(startdate.getDate()) + " " + _monthsShort[startdate.getMonth()]);
    modal.find('#eventModalEndDate').text(pad2(endDate.getDate()) + " " + _monthsShort[endDate.getMonth()]);
    modal.find('.modal-body').html(description);
});

$(document).ready(function () {
    var year = parseInt($("#year").text());
    DisplayCalendar(year);
});

$(window).resize(function () {
    ZoomIt();
});


function DisplayCalendar(year) {
    DrawYearPlanner(year, $("#calendarData").data("json"));
}

function PositionEvents() {
    $('.eventitem').each(function () {
        $content.show();

        var calendar = $(this).data("calendar");
        var startdate = parseISODate($(this).data("start-date"));
        var cellName = "#d" + dateToYYYYLLDD(startdate);
        var cell = $(cellName);
        var cellWidth = parseFloat(cell.css("width"));
        var padLeft = cellWidth / 6;

        if (calendar == "Tides") {
            var tideWidth = parseFloat($(this).css("width"));
            $(this).css({ top: cell.offset().top + 2, left: cell.offset().left + cellWidth - tideWidth - 1 });
        } else {
            var duration = $(this).data("duration");
            var description = $(this).data("description");

            var overlapEvents = $('.cal[data-start-date="' + $(this).data("start-date") + '"]');
            var overlapEventCount = overlapEvents.length;
            var overlapEventIndex = overlapEvents.index($(this));

            var evtwidth = (cellWidth - padLeft) / overlapEventCount;
            var startleft = cell.offset().left + padLeft + 1 + (evtwidth * overlapEventIndex);

            var evtheight = parseFloat(cell.css("height")) * duration;
            $(this).find(".desc").html(description);
            $(this).css({ top: cell.offset().top, left: startleft, width: evtwidth, height: evtheight });
        }
    });
}

function buildCalendarKeyHtml(result) {
    var html = "";
    $.each(result, function (i, cal) {
        if (cal.Name != "Tides") {
            var safeName = cal.Name.replace(/ /g, "_");
            html += "<li class='cal_" + safeName + "'>";
            html += "<span style='display:inline-block;width:12px;height:12px;background:" + cal.BackgroundColor + ";border-radius:2px;margin-right:4px;vertical-align:middle;'></span>";
            html += cal.Name + "</li>";
        }
    });
    return html;
}

function DrawYearPlanner(year, result) {
    $("#calendar").html(buildYearHtml(year));
    $("#calenderStyles").html(buildCalendarStylesHtml(result));
    $("#key").html(buildCalendarKeyHtml(result));
    $("#events").html(buildEventsHtml(result));
    PositionEvents();
    $("#agenda").html(buildAgendaHtml(result));
    ZoomIt();
    window.status = 'ready';
}

function buildAgendaHtml(result) {
    if ($("#agenda").length == 0) return;

    result = jQuery.grep(result, function (cal) {
        return cal.Name !== "Tides";
    });

    eventdata = [];

    $.each(result, function (i, cal) {
        var cssClass = "cal_" + cal.Name.replace(/ /g, "_");

        $.each(cal.Events, function (x, event) {
            var start = parseISODate(event.StartDate);
            var end = parseISODate(event.EndDate);

            var data = {
                calendar: cal.Name,
                startDate: event.StartDate,
                day: start.getDate(),
                month: _monthsShort[start.getMonth()],
                summary: event.Summary,
                description: event.Description,
                cssClass: cssClass
            };

            if (start.getHours() == 0 && start.getMinutes() == 0) {
                data.period = start.getDate() + " " + _monthsShort[start.getMonth()] + " - " + end.getDate() + " " + _monthsShort[end.getMonth()];
            } else {
                data.period = jsDayToShort(start.getDay()) + " " + pad2(start.getHours()) + ":" + pad2(start.getMinutes());
            }

            eventdata.push(data);
        });
    });

    eventdata.sort(function (a, b) {
        return parseISODate(a.startDate) - parseISODate(b.startDate);
    });

    var agendaTemplate = $("#agenda-template").html();
    var rowspercol = Math.ceil(eventdata.length / 4);
    var html = "";

    for (var i = 0; i < 4; i++) {
        var tmp = eventdata.splice(0, rowspercol);
        html += "<div><div>";
        $.each(tmp, function (i, cal) {
            html += Mustache.render(agendaTemplate, cal);
        });
        html += "</div></div>";
    }

    return html;
}


function buildCalendarStylesHtml(result) {
    var html = "";
    $.each(result, function (i, cal) {
        if (cal.Name != "Tides") {
            var safeName = cal.Name.replace(/ /g, "_");
            var cssClass = "cal_" + safeName;
            html += " .eventitem." + cssClass + "{ background-color:" + cal.BackgroundColor + "; }";
            html += " .agendarow." + cssClass + " .date { border-left-color: " + cal.BackgroundColor + " !important; }";
        }
    });
    return html;
}

function buildEventsHtml(result) {
    var eventTemplate = $("#event-template").html();
    var tideTemplate = $("#tide-template").html();
    var html = "";

    $.each(result, function (i, cal) {
        $.each(cal.Events, function (x, event) {
            if (cal.Name == "Tides") {
                var data = {
                    calendar: cal.Name,
                    summary: event.Summary,
                    startDate: event.StartDate,
                    cssClass: "tide-" + event.Summary
                };
                html += Mustache.render(tideTemplate, data);
            } else {
                var data = {
                    calendar: cal.Name,
                    startDate: event.StartDate,
                    endDate: event.EndDate,
                    duration: event.Duration,
                    summary: event.Summary,
                    description: event.Description,
                    cssClass: "cal_" + cal.Name.replace(/ /g, "_")
                };
                html += Mustache.render(eventTemplate, data);
            }
        });
    });

    return html;
}

function buildMonthNamesHtml() {
    var html = "";
    for (var i = 0; i < 12; i++) {
        html += "<div class='text-center month'>" + _months[i] + "</div>";
    }
    return html;
}

function buildMonthsHtml(day, year) {
    var html = "";
    for (var month = 1; month <= 12; month++) {
        var jsDate = new Date(year, month - 1, day);
        var actualDay = jsDate.getDate();
        var actualMonth = jsDate.getMonth() + 1;

        html += "<div id='d" + year + pad2(actualMonth) + pad2(actualDay) + "'";

        if (day !== actualDay) {
            html += " class='nodate'>&nbsp;</div>";
        } else {
            var jsDay = jsDate.getDay(); // 0=Sun, 1=Mon, …, 6=Sat
            var isWeekend = (jsDay === 6) || (jsDay === 0);
            if (isWeekend) {
                html += " class='weekend'";
            }
            html += ">";
            html += jsDayToShort(jsDay);
            html += "</div>";
        }
    }
    return html;
}

function buildYearHtml(year) {
    var html = "";

    html += "<div class='calrow monthrow'>";
    html += "<div class='noborder'>&nbsp;</div>";
    html += buildMonthNamesHtml();
    html += "<div class='noborder'>&nbsp;</div>";
    html += "</div>";

    for (var i = 1; i < 32; i++) {
        html += "<div class='calrow dayrow'>";
        html += "<div class='text-center day'>" + i + "</div>";
        html += buildMonthsHtml(i, year);
        html += "<div class='text-center day'>" + i + "</div>";
        html += "</div>";
    }

    return html;
}

function ZoomIt() {
    var isPdf = $("#content").data("ispdf").toLowerCase() == "true";
    if (!isPdf) {
        var perc = $(window).width() / $(document).width();
        $content.css("transform-origin", "top left");
        $content.css("transform", "scale(" + perc + ")");
    }
}
