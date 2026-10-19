(function () {
    'use strict';
    document.querySelectorAll('input[type="password"]').forEach(function (input) {
        var wrapper = document.createElement('div');
        wrapper.className = 'password-control';
        input.parentNode.insertBefore(wrapper, input);
        wrapper.appendChild(input);
        var toggle = document.createElement('button');
        toggle.type = 'button';
        toggle.className = 'password-toggle';
        toggle.textContent = 'Show';
        toggle.setAttribute('aria-label', 'Show ' + (input.id === 'ConfirmPassword' ? 'confirmation password' : 'password'));
        toggle.setAttribute('aria-pressed', 'false');
        toggle.setAttribute('aria-controls', input.id);
        wrapper.appendChild(toggle);
        toggle.addEventListener('click', function () {
            var visible = input.type === 'password';
            input.type = visible ? 'text' : 'password';
            toggle.textContent = visible ? 'Hide' : 'Show';
            toggle.setAttribute('aria-pressed', String(visible));
            toggle.setAttribute('aria-label', (visible ? 'Hide ' : 'Show ') + (input.id === 'ConfirmPassword' ? 'confirmation password' : 'password'));
        });
        var caps = document.createElement('small');
        caps.className = 'caps-warning';
        caps.textContent = 'Caps Lock is on';
        caps.hidden = true;
        wrapper.insertAdjacentElement('afterend', caps);
        input.addEventListener('keyup', function (event) { caps.hidden = !(event.getModifierState && event.getModifierState('CapsLock')); });
        input.addEventListener('blur', function () { caps.hidden = true; });
    });
    var password = document.querySelector('input[autocomplete="new-password"]');
    if (password && document.getElementById('password-help')) {
        var meter = document.createElement('div');
        meter.className = 'password-strength';
        meter.hidden = true;
        meter.innerHTML = '<span class="strength-track"><span></span></span><small role="status"></small>';
        document.getElementById('password-help').insertAdjacentElement('afterend', meter);
        password.addEventListener('input', function () {
            var value = password.value;
            meter.hidden = !value;
            var score = [value.length >= 6, /[A-Z]/.test(value), /[a-z]/.test(value), /[0-9]/.test(value), /[^a-zA-Z0-9]/.test(value)].filter(Boolean).length;
            meter.querySelector('.strength-track > span').style.width = (value ? score * 20 : 0) + '%';
            meter.classList.toggle('is-complete', score === 5);
            meter.querySelector('small').textContent = !value ? '' : score === 5 ? 'Password meets the account requirements.' : 'Requirements met: ' + score + ' of 5';
        });
    }
    var search = document.getElementById('candidate-search');
    if (search) {
        search.addEventListener('input', function () {
            var term = search.value.trim().toLocaleLowerCase();
            var cards = document.querySelectorAll('[data-candidate-card]');
            var visible = 0;
            cards.forEach(function (card) {
                var match = card.textContent.toLocaleLowerCase().indexOf(term) !== -1;
                card.hidden = !match;
                if (match) visible++;
            });
            document.getElementById('candidate-search-status').textContent = visible ? visible + ' nomination' + (visible === 1 ? '' : 's') + ' shown' : 'No matching candidates. Try another name or position.';
        });
    }
    document.querySelectorAll('[data-countdown]').forEach(function (element) {
        var end = new Date(element.dataset.countdown).getTime();
        if (!Number.isFinite(end)) return;
        function tick() {
            var total = Math.max(0, Math.floor((end - Date.now()) / 1000));
            if (!total) { element.textContent = 'The election schedule has changed. Refresh to see the latest status.'; return; }
            var days = Math.floor(total / 86400), hours = Math.floor(total % 86400 / 3600), minutes = Math.floor(total % 3600 / 60);
            element.textContent = days + 'd  ' + hours + 'h  ' + minutes + 'm remaining';
        }
        tick();
        window.setInterval(tick, 30000);
    });
    // Search administration records without changing server-side data.
    document.querySelectorAll('[data-table-filter]').forEach(function (input) {
        var table = document.getElementById(input.dataset.tableFilter);
        if (!table) return;
        var rows = Array.from(table.querySelectorAll('tbody tr'));
        var panel = input.closest('.management-panel');
        function filterRows() {
            var term = input.value.trim().toLocaleLowerCase(), visible = 0;
            rows.forEach(function (row) {
                row.hidden = row.textContent.toLocaleLowerCase().indexOf(term) === -1;
                if (!row.hidden) visible++;
            });
            var status = panel.querySelector('[data-table-status]');
            if (status) status.textContent = visible + ' of ' + rows.length + ' records';
            var empty = panel.querySelector('.table-no-results');
            if (empty) empty.hidden = visible !== 0;
        }
        input.addEventListener('input', filterRows);
    });
    // A single accessible confirmation dialog serves every candidate.
    var dialogElement = document.getElementById('voteConfirmation');
    var selectedForm = null;
    var submitted = false;
    document.querySelectorAll('[data-vote-form]').forEach(function (form) {
        form.addEventListener('submit', function (event) {
            if (form.dataset.confirmed === 'true') return;
            if (!window.bootstrap || !dialogElement) {
                if (!window.confirm('Confirm your vote for ' + form.dataset.candidateName + '? This choice cannot be changed.')) event.preventDefault();
                return;
            }
            event.preventDefault();
            selectedForm = form;
            document.getElementById('vote-candidate-name').textContent = form.dataset.candidateName;
            document.getElementById('vote-position-name').textContent = form.dataset.positionName;
            var card = form.closest('.candidate-card');
            if (card) card.classList.add('is-selected');
            bootstrap.Modal.getOrCreateInstance(dialogElement).show();
        });
    });
    if (dialogElement && window.bootstrap) {
        dialogElement.addEventListener('hidden.bs.modal', function () {
            if (selectedForm && !submitted) {
                var card = selectedForm.closest('.candidate-card');
                if (card) card.classList.remove('is-selected');
                var trigger = selectedForm.querySelector('[type="submit"]');
                if (trigger) trigger.focus();
                selectedForm = null;
            }
        });
        document.getElementById('confirm-vote').addEventListener('click', function () {
            if (!selectedForm || submitted) return;
            selectedForm.dataset.confirmed = 'true';
            submitted = true;
            this.disabled = true;
            this.textContent = 'Recording your vote...';
            selectedForm.requestSubmit();
        });
    }
    // Disable only the submit control once the form has passed its validation.
    document.querySelectorAll('form').forEach(function (form) {
        form.addEventListener('submit', function (event) {
            if (event.defaultPrevented) return;
            if (window.jQuery && jQuery.fn.valid && !jQuery(form).valid()) return;
            var button = event.submitter || form.querySelector('[type="submit"]');
            if (!button) return;
            var isInput = button.tagName === 'INPUT';
            var original = isInput ? button.value : button.innerHTML;
            window.setTimeout(function () {
                if (event.defaultPrevented) return;
                button.disabled = true;
                if (isInput) button.value = 'Please wait...';
                else button.textContent = 'Please wait...';
            }, 0);
            window.addEventListener('pageshow', function () {
                button.disabled = false;
                if (isInput) button.value = original;
                else button.innerHTML = original;
            }, { once: true });
        });
    });
}());



