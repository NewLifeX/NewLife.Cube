(function (factory) {
    if (typeof define === 'function' && define.amd) {
        // AMD. Register as anonymous module.
        define(['jquery'], factory);
    } else if (typeof exports === 'object') {
        // Node / CommonJS
        factory(require('jquery'));
    } else {
        // Browser globals.
        factory(jQuery);
    }
})(function ($) {

    'use strict';

    //if (typeof ChineseDistricts === 'undefined') {
    //    throw new Error('The file "city-picker.data.js" must be included first!');
    //}

    var NAMESPACE = 'citypicker';
    var EVENT_CHANGE = 'change.' + NAMESPACE;
    var PROVINCE = 'province';
    var CITY = 'city';
    var DISTRICT = 'district';
    var TOWN = 'town';

    function CityPicker(element, options) {
        this.$element = $(element);
        this.$dropdown = null;
        var strValue = 0;
        var val = $(element).data("path") || $(element).val();
        if (val != undefined) {
            strValue = (val + '').split('/');
        }

        var cssOptions = { 'level': $(element).data("level"), 'autoPost': $(element).data("autopost") };
        if (strValue.length > 0) {
            $.each(strValue, function (i, n) {
                if (i == 0) {
                    cssOptions.province = parseInt(n);
                }
                if (i == 1) {
                    cssOptions.city = parseInt(n);
                }
                if (i == 2) {
                    cssOptions.district = parseInt(n);
                }
                if (i == 3) {
                    cssOptions.town = parseInt(n);
                }
            });
        }
        this.options = $.extend({}, CityPicker.DEFAULTS, cssOptions, $.isPlainObject(options) && options);
        this.active = false;
        this.dems = [];
        this.needBlur = false;
        this.initFullPath();
        this.init();
    }

    CityPicker.prototype = {
        constructor: CityPicker,
        initFullPath: function () {
            var code = 0;
            if (this.options.province !== '') {
                code = this.options.province;
            }
            if (this.options.city !== '') {
                code = this.options.city;
            }
            if (this.options.district !== '') {
                code = this.options.district;
            }
            if (this.options.town !== '') {
                code = this.options.town;
            }
            var context = this;
            if (code && code !== 0) {
                $.ajaxSettings.async = false;
                $.getJSON(this.options.areaParents + code, function (json) {
                    $.each(json.data, function (i, n) {
                        if (n.level === 1) {
                            context.options.province = n.id;
                        }
                        if (n.level === 2) {
                            context.options.city = n.id;
                        }
                        if (n.level === 3) {
                            context.options.district = n.id;
                        }
                        if (n.level === 4) {
                            context.options.town = n.id;
                        }
                    });
                    $.ajaxSettings.async = true;
                });

            }
        },

        init: function () {

            this.defineDems();

            this.render();

            this.bind();

            this.active = true;
        },

        render: function () {
            var p = this.getPosition(),
                placeholder = this.$element.attr('placeholder') || this.options.placeholder,
                closebtn = "<span class='cube_clear'><img src='/Content/city_picker/images/clear.png'></span>",// 清空按钮
                textspan = '<span class="city-picker-span" style="' +
                    this.getWidthStyle(p.width) + 'height:' +
                    p.height + 'px;line-height:' + (p.height - 1) + 'px;float:left;">' +
                    (placeholder ? '<span class="placeholder">' + placeholder + '</span>' : '') +
                    closebtn +
                    '<span class="title"></span><div class="arrow"></div>' + '</span>',
                domclear = '<input name=\'' + $(this.$element).attr("id") + '\' type=\'text\' class=\'.picker-clear\' style=\'float:left;display:none\' value=\'\'/>';
            var dropdown = '<div class="city-picker-dropdown" style="left:' + p.left + 'px;top:' + (p.top + p.height) + 'px;' + //style="left:0px;top:100%;' 备份
                this.getWidthStyle(p.width, true) + '">' +
                '<div class="city-select-wrap">' +
                '<div class="city-select-tab">' +
                '<a class="active" data-count="province">省份</a>' +
                (this.includeDem('city') ? '<a data-count="city">城市</a>' : '') +
                (this.includeDem('district') ? '<a data-count="district">区县</a>' : '') +
                (this.includeDem('town') ? '<a data-count="town">乡镇街道</a>' : '') + '</div>' +
                '<div class="city-select-content">' +
                '<div class="city-select province" data-count="province"></div>' +
                (this.includeDem('city') ? '<div class="city-select city" data-count="city"></div>' : '') +
                (this.includeDem('district') ? '<div class="city-select district" data-count="district"></div>' : '') +
                (this.includeDem('town') ? '<div class="city-select town" data-count="town"></div>' : '') +
                '</div></div>';

            this.$element.addClass('city-picker-input');
            this.$textspan = $(textspan).insertAfter(this.$element);
            this.$dropdown = $(dropdown).insertAfter(this.$textspan);
            this.$domclear = $(domclear).insertAfter(this.$textspan);
            var $select = this.$dropdown.find('.city-select');

            // 清空按钮
            $('.cube_clear').find('img').bind('click', function () {
                $(this).parent().parent().parent().find("input").first().citypicker("reset");
                $(this).parent().css('display', 'none');
                event.stopPropagation();
            });

            // setup this.$province, this.$city and/or this.$district object
            $.each(this.dems, $.proxy(function (i, type) {
                this['$' + type] = $select.filter('.' + type + '');
            }, this));

            this.refresh();
        },

        refresh: function (force) {
            // clean the data-item for each $select
            var $select = this.$dropdown.find('.city-select');
            $select.data('item', null);
            // parse value from value of the target $element
            var val = this.$element.data("path") + '';
            val = val.split('/');
            $.each(this.dems, $.proxy(function (i, type) {
                if (val[i] && i < val.length) {
                    this.options[type] = val[i];
                } else if (force) {
                    this.options[type] = '';
                }
                this.output(type);
            }, this));
            this.tab(PROVINCE);
            this.feedText();
            this.feedVal();
        },

        defineDems: function () {
            var stop = false;
            $.each([PROVINCE, CITY, DISTRICT, TOWN], $.proxy(function (i, type) {
                if (!stop) {
                    this.dems.push(type);
                }
                if (type === this.options.level) {
                    stop = true;
                }
            }, this));
        },

        includeDem: function (type) {
            return $.inArray(type, this.dems) !== -1;
        },

        getPosition: function () {
            var p, h, w, s, pw;
            p = this.$element.position();
            s = this.getSize(this.$element);
            h = s.height;
            w = s.width;
            if (this.options.responsive) {
                pw = this.$element.offsetParent().width();
                if (pw) {
                    w = w / pw;
                    if (w > 0.99) {
                        w = 1;
                    }
                    w = w * 100 + '%';
                }
            }

            return {
                top: p.top || 0,
                left: p.left || 0,
                height: h,
                width: w
            };
        },

        getSize: function ($dom) {
            var $wrap, $clone, sizes;
            if (!$dom.is(':visible')) {
                $wrap = $("<div />").appendTo($("body"));
                $wrap.css({
                    "position": "absolute !important",
                    "visibility": "hidden !important",
                    "display": "block !important"
                });

                $clone = $dom.clone().appendTo($wrap);

                sizes = {
                    width: $clone.outerWidth(),
                    height: $clone.outerHeight()
                };

                $wrap.remove();
            } else {
                sizes = {
                    width: $dom.outerWidth(),
                    height: $dom.outerHeight()
                };
            }

            return sizes;
        },

        getWidthStyle: function (w, dropdown) {
            if (this.options.responsive && !$.isNumeric(w)) {
                return 'width:' + w + ';';
            } else {
                return 'width:' + (dropdown ? Math.max(320, w) : w) + 'px;';
            }
        },

        bind: function () {
            var $this = this;

            $(document).on('click', (this._mouteclick = function (e) {
                var $target = $(e.target);
                var $dropdown, $span, $input;
                // 设置清除按钮
                if ($target.is($)) $target.parents('.form-group').find('.cube_clear').css('display', 'block');
                if ($target.is('.city-picker-span')) {
                    $span = $target;
                    var vs = $this.getPath().split('/');
                    if (vs.length != 0) {
                        $this.$element.parent().children().find(".city-select").attr("style", "display:none");
                        $this.$element.parent().children().find(".city-select").eq(vs.length - 1).attr("style", "display:block");
                        $this.$element.parent().children().find(".city-select-tab >a").removeClass("active");
                        $this.$element.parent().children().find(".city-select-tab >a").eq(vs.length - 1).addClass("active");
                    }

                } else if ($target.is('.city-picker-span *')) {
                    $span = $target.parents('.city-picker-span');
                }
                if ($target.is('.city-picker-input')) {
                    $input = $target;
                }
                if ($target.is('.city-picker-dropdown')) {
                    $dropdown = $target;
                } else if ($target.is('.city-picker-dropdown *')) {
                    $dropdown = $target.parents('.city-picker-dropdown');
                }
                //if ($target.is('input[class=\'.picker-clear\']')) {
                //    $(this).find('.active').removeClass('active');
                //    $(this).find('.title').attr('style','display:none').attr('title','').html('');
                //    $(this).find('.placeholder').attr("style",'dispaly:block');
                //}
                if ((!$input && !$span && !$dropdown) ||
                    ($span && $span.get(0) !== $this.$textspan.get(0)) ||
                    ($input && $input.get(0) !== $this.$element.get(0)) ||
                    ($dropdown && $dropdown.get(0) !== $this.$dropdown.get(0))) {
                    $this.close(true);
                }

            }));

            this.$element.on('change', (this._changeElement = $.proxy(function () {
                this.close(true);
                this.refresh(true);
            }, this))).on('focus', (this._focusElement = $.proxy(function () {
                this.needBlur = true;
                this.open();
            }, this))).on('blur', (this._blurElement = $.proxy(function () {
                if (this.needBlur) {
                    this.needBlur = false;
                    this.close(true);
                }
            }, this)));

            this.$textspan.on('click', function (e) {
                var $target = $(e.target), type;
                $this.needBlur = false;
                if ($target.is('.select-item')) {
                    type = $target.data('count');
                    $this.open(type);
                } else {
                    if ($this.$dropdown.is(':visible')) {
                        $this.close();
                    } else {
                        $this.open();
                    }
                }
            }).on('mousedown', function () {
                $this.needBlur = false;
            });

            this.$dropdown.on('click', '.city-select a', function () {
                var $select = $(this).parents('.city-select');
                var $active = $select.find('a.active');
                var last = $select.next().length === 0;
                $active.removeClass('active');
                $(this).addClass('active');
                if ($active.data('code') !== $(this).data('code')) {
                    $select.data('item', {
                        address: $(this).attr('title'), code: $(this).data('code')
                    });
                    $(this).trigger(EVENT_CHANGE);
                    $this.feedText();
                    $this.feedVal(true);
                    if (last) {
                        $this.close();
                        $this.options.clickShow();
                    }
                    if ($this.options.autoPost) {
                        $("form")[0].submit();
                    }
                }
            }).on('click', '.city-select-tab a', function () {
                if (!$(this).hasClass('active')) {
                    var type = $(this).data('count');
                    $this.tab(type);
                }
            }).on('mousedown', function () {
                $this.needBlur = false;
            });

            if (this.$province) {
                this.$province.on(EVENT_CHANGE, (this._changeProvince = $.proxy(function () {
                    this.output(CITY);
                    this.output(DISTRICT);
                    this.output(TOWN);
                    this.tab(CITY);
                }, this)));
            }

            if (this.$city) {
                this.$city.on(EVENT_CHANGE, (this._changeCity = $.proxy(function () {
                    this.output(DISTRICT);
                    this.output(TOWN);
                    this.tab(DISTRICT);
                }, this)));
            }

            if (this.$district) {
                this.$district.on(EVENT_CHANGE, (this._changeDistrict = $.proxy(function () {
                    this.output(TOWN);
                    this.tab(TOWN);
                }, this)));
            }
        },

        open: function (type) {
            type = type || PROVINCE;
            this.$dropdown.show();
            this.$textspan.addClass('open').addClass('focus');
            this.tab(type);
        },

        close: function (blur) {
            this.$dropdown.hide();
            this.$textspan.removeClass('open');
            if (blur) {
                this.$textspan.removeClass('focus');
            }
        },

        unbind: function () {

            $(document).off('click', this._mouteclick);

            this.$element.off('change', this._changeElement);
            this.$element.off('focus', this._focusElement);
            this.$element.off('blur', this._blurElement);

            this.$textspan.off('click');
            this.$textspan.off('mousedown');

            this.$dropdown.off('click');
            this.$dropdown.off('mousedown');

            if (this.$province) {
                this.$province.off(EVENT_CHANGE, this._changeProvince);
            }

            if (this.$city) {
                this.$city.off(EVENT_CHANGE, this._changeCity);
            }
        },

        getText: function () {
            var text = '';
            this.$dropdown.find('.city-select')
                .each(function () {
                    var item = $(this).data('item'),
                        type = $(this).data('count');
                    if (item) {
                        text += ($(this).hasClass('province') ? '' : '/') + '<span class="select-item" data-count="' +
                            type + '" data-code="' + item.code + '" name="' + type + 'id">' + item.address + '</span>';
                    }
                });
            return text;
        },

        getPlaceHolder: function () {
            return this.$element.attr('placeholder') || this.options.placeholder;
        },

        feedText: function () {
            var text = this.getText();
            if (text) {
                this.$textspan.find('>.placeholder').hide();
                this.$textspan.find('>.title').html(this.getText()).show();
                $("input[name='" + $(this.$element).attr("id") + "']").val(this.getVal());
                // 控制清除按钮
                this.$textspan.find('>.cube_clear').find('img').parent().css('display', 'block');
            } else {
                this.$textspan.find('>.placeholder').text(this.getPlaceHolder()).show();
                this.$textspan.find('>.title').html('').hide();
                $("input[name='" + $(this.$element).attr("id") + "']").val("");
                // 控制清除按钮
                this.$textspan.find('>.cube_clear').find('img').parent().css('display', 'none');
            }
        },

        getCode: function (count) {
            var obj = {}, arr = [];
            this.$textspan.find('.select-item')
                .each(function () {
                    var code = $(this).data('code');
                    var count = $(this).data('count');
                    obj[count] = code;
                    arr.push(code);
                });
            return count ? obj[count] : arr.join('/');
        },

        getPath: function () {
            // 遍历所有的城市选择器，组装路径
            var text = '';
            this.$dropdown.find('.city-select')
                .each(function () {
                    var item = $(this).data('item');
                    if (item) {
                        text += ($(this).hasClass('province') ? '' : '/') + item.code;
                    }
                });
            return text;
        },

        getVal: function () {
            // 遍历所有的城市选择器，只要最后一个的code
            var text = '';
            this.$dropdown.find('.city-select')
                .each(function () {
                    var item = $(this).data('item');
                    if (item) {
                        text = item.code;
                    }
                });
            return text;
        },

        feedVal: function (trigger) {
            this.$element.val(this.getVal());
            this.$element.data('path', this.getPath());
            if (trigger) {
                this.$element.trigger('cp:updated');
            }
        },

        output: function (type) {
            var options = this.options;
            //var placeholders = this.placeholders;
            var $select = this['$' + type];
            var data = type === PROVINCE ? {} : [];
            var item;
            var districts;
            var code;
            var matched = null;
            var value;

            if (!$select || !$select.length) {
                return;
            }

            item = $select.data('item');

            value = (item ? item.address : null) || parseInt(options[type]);

            code = (
                type === PROVINCE ? 86 :
                    type === CITY ? this.$province && this.$province.find('.active').data('code') :
                        type === DISTRICT ? this.$city && this.$city.find('.active').data('code') :
                            type === TOWN ? this.$district && this.$district.find('.active').data('code') : code
            );

            //districts = $.isNumeric(code) ? ChineseDistricts[code] : null;
            //console.log(districts);
            //var result = this.getData(code);
            //console.log(result);

            districts = $.isNumeric(code) ? this.getData(code) : null;
            if ($.isPlainObject(districts)) {
                $.each(districts, function (code, address) {
                    var provs;
                    if (type === PROVINCE) {
                        provs = [];
                        for (var i = 0; i < address.length; i++) {
                            if (parseInt(address[i].code) === value) {
                                matched = {
                                    code: address[i].code,
                                    address: address[i].address
                                };
                            }
                            provs.push({
                                code: address[i].code,
                                address: address[i].address,
                                selected: address[i].code === value
                            });
                        }
                        data[code] = provs;
                    } else {
                        code = parseInt(code);
                        if (code === value) {
                            matched = {
                                code: code,
                                address: address
                            };
                        }
                        data.push({
                            code: code,
                            address: address,
                            selected: code === value
                        });
                    }
                });
            }

            $select.html(type === PROVINCE ? this.getProvinceList(data) :
                this.getList(data, type));
            $select.data('item', matched);
        },

        getProvinceList: function (data) {
            var list = [],
                $this = this,
                simple = this.options.simple;

            $.each(data, function (i, n) {
                list.push('<dl class="clearfix">');
                list.push('<dt>' + i + '</dt><dd>');
                $.each(n, function (j, m) {
                    list.push(
                        '<a' +
                        ' title="' + (m.address || '') + '"' +
                        ' data-code="' + (m.code || '') + '"' +
                        ' class="' +
                        (m.selected ? ' active' : '') +
                        '">' +
                        (simple ? $this.simplize(m.address, PROVINCE) : m.address) +
                        '</a>');
                });
                list.push('</dd></dl>');
            });

            return list.join('');
        },

        getList: function (data, type) {
            var list = [],
                $this = this,
                simple = this.options.simple;
            list.push('<dl class="clearfix"><dd>');

            $.each(data, function (i, n) {
                list.push(
                    '<a' +
                    ' title="' + (n.address || '') + '"' +
                    ' data-code="' + (n.code || '') + '"' +
                    ' class="' +
                    (n.selected ? ' active' : '') +
                    '">' +
                    (simple ? $this.simplize(n.address, type) : n.address) +
                    '</a>');
            });
            list.push('</dd></dl>');

            return list.join('');
        },

        simplize: function (address, type) {
            address = address || '';
            if (type === PROVINCE) {
                return address.replace(/[省,市,自治区,壮族,回族,维吾尔]/g, '');
            } else if (type === CITY) {
                return address.replace(/[市,地区,回族,蒙古,苗族,白族,傣族,景颇族,藏族,彝族,壮族,傈僳族,布依族,侗族]/g, '')
                    .replace('哈萨克', '').replace('自治州', '').replace(/自治县/, '');
            } else if (type === DISTRICT) {
                return address.length > 2 ? address.replace(/[市,区,县,旗]/g, '') : address;
            } else if (type === TOWN) {
                return address.length > 3 ? address.replace(/[乡,镇,街道]/g, '') : address;
            }
        },

        tab: function (type) {
            var $selects = this.$dropdown.find('.city-select');
            var $tabs = this.$dropdown.find('.city-select-tab > a');
            var $select = this['$' + type];
            var $tab = this.$dropdown.find('.city-select-tab > a[data-count="' + type + '"]');
            if ($select) {
                $selects.hide();
                $select.show();
                $tabs.removeClass('active');
                $tab.addClass('active');
            }
        },

        reset: function () {
            this.$element.data('path', '');
            this.$element.val(null).trigger('change');
        },

        destroy: function () {
            this.unbind();
            this.$element.removeData(NAMESPACE).removeClass('city-picker-input');
            this.$textspan.remove();
            this.$dropdown.remove();
        },

        getData: function (code) {
            if (!$.isNumeric(code)) {
                return null;
            }
            var result = {};
            var temp = 0;
            if (code !== 86) {
                temp = code;
            }
            $.ajaxSettings.async = false
            $.getJSON(this.options.dataUrl + temp, function (json) {
                $.each(json.data, function (i, n) {
                    if (n.bigArea == undefined) {
                        result[n.id] = n.name;
                    }
                    else {
                        if (n.bigArea in result) {
                            result[n.bigArea].push({ code: n.id, address: n.name });
                        }
                        else {
                            result[n.bigArea] = [{ code: n.id, address: n.name }];
                        }
                    }
                });
            });
            $.ajaxSettings.async = true;
            return result;
        }
    };

    CityPicker.DEFAULTS = {
        simple: false,
        responsive: false,
        placeholder: '请选择省/市/区',
        level: 'district',
        province: '',
        city: '',
        district: '',
        town: '',
        dataUrl: '/Cube/AreaChilds?id=',
        areaParents: '/Cube/AreaParents?id=',
        autoPost: false,
        clickShow: function () {
            //console.log("点击处理了");
        }
    };

    CityPicker.setDefaults = function (options) {
        $.extend(CityPicker.DEFAULTS, options);
    };

    // Save the other citypicker
    CityPicker.other = $.fn.citypicker;

    // Register as jQuery plugin
    $.fn.citypicker = function (option) {
        var args = [].slice.call(arguments, 1);
        var result = null;
        this.each(function () {
            var $this = $(this);
            var data = $this.data(NAMESPACE);
            var options;
            var fn;

            if (!data) {
                if (/destroy/.test(option)) {
                    return;
                }
                options = $.extend({}, $this.data(), $.isPlainObject(option) && option);
                $this.data(NAMESPACE, (data = new CityPicker(this, options)));
            }

            if (typeof option === 'string' && $.isFunction(fn = data[option])) {
                result = fn.apply(data, args);
            }
        });
        if (result != null) {
            return result;
        }
        return this;
    };

    $.fn.citypicker.Constructor = CityPicker;
    $.fn.citypicker.setDefaults = CityPicker.setDefaults;

    // No conflict
    $.fn.citypicker.noConflict = function () {
        $.fn.citypicker = CityPicker.other;
        return this;
    };

    $(function () {
        $('[data-toggle="city-picker"]').citypicker();
    });
});