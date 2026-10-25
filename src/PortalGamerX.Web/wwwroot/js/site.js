(function () {
  'use strict';

  function formatBrl(v) {
    return 'R$ ' + Number(v).toFixed(2).replace('.', ',').replace(/\B(?=(\d{3})+(?!\d))/g, '.');
  }

  function el(tag, cls, text) {
    var e = document.createElement(tag);
    if (cls) e.className = cls;
    if (text !== undefined) e.textContent = text; // textContent: nunca interpreta HTML
    return e;
  }

  // Confirmação declarativa (compatível com CSP, sem JS inline): <form data-confirm="Mensagem">
  document.addEventListener('submit', function (ev) {
    var msg = ev.target && ev.target.getAttribute && ev.target.getAttribute('data-confirm');
    if (msg && !window.confirm(msg)) ev.preventDefault();
  });

  // Galeria de jogo
  document.querySelectorAll('[data-gallery]').forEach(function (g) {
    var main = g.querySelector('[data-gallery-main]');
    g.querySelectorAll('[data-gallery-thumb]').forEach(function (t) {
      t.addEventListener('click', function () {
        main.src = t.getAttribute('data-full');
        g.querySelectorAll('[data-gallery-thumb]').forEach(function (x) { x.classList.remove('active'); });
        t.classList.add('active');
      });
    });
  });

  // Máscara simples de CEP
  document.querySelectorAll('input[data-mask="cep"]').forEach(function (i) {
    i.addEventListener('input', function () {
      var d = i.value.replace(/\D/g, '').slice(0, 8);
      i.value = d.length > 5 ? d.slice(0, 5) + '-' + d.slice(5) : d;
    });
  });

  // Estimativa de frete no carrinho (informativa; o valor final é recalculado no servidor)
  var est = document.querySelector('[data-shipping-estimate]');
  if (est) {
    var out = est.querySelector('[data-shipping-result]');
    est.addEventListener('submit', function (ev) {
      ev.preventDefault();
      out.textContent = 'Calculando...';
      fetch(est.action, { method: 'POST', body: new FormData(est), credentials: 'same-origin' })
        .then(function (r) { return r.json(); })
        .then(function (data) {
          out.textContent = '';
          if (!data.success) { out.appendChild(el('div', 'text-danger small', data.error || 'Frete indisponível.')); return; }
          if (data.simulated) out.appendChild(el('div', 'badge bg-warning text-dark mb-1', 'VALORES SIMULADOS (modo de desenvolvimento)'));
          data.options.forEach(function (o) {
            out.appendChild(el('div', 'small', o.name + ' (' + o.carrier + ') - ' + o.priceText + ' - ' + o.days + ' dia(s) úteis'));
          });
        })
        .catch(function () { out.textContent = 'Não foi possível calcular o frete agora.'; });
    });
  }

  // Checkout: carrega opções de frete ao escolher o endereço
  var co = document.querySelector('[data-checkout]');
  if (co) {
    var list = co.querySelector('[data-shipping-options]');
    var hidden = co.querySelector('input[name="ShippingServiceCode"]');
    var submit = co.querySelector('[data-checkout-submit]');
    var base = parseFloat(co.getAttribute('data-base-total')) || 0;
    var totalEl = co.querySelector('[data-total]');
    var shipEl = co.querySelector('[data-shipping-total]');

    function setShipping(price) {
      if (shipEl) shipEl.textContent = price === null ? 'a calcular' : (price === 0 ? 'Grátis' : formatBrl(price));
      if (totalEl) totalEl.textContent = price === null ? formatBrl(base) : formatBrl(base + price);
    }

    function loadShipping(addressId) {
      hidden.value = '';
      submit.disabled = true;
      setShipping(null);
      list.textContent = 'Consultando frete...';
      fetch(co.getAttribute('data-shipping-url') + '?addressId=' + encodeURIComponent(addressId), { credentials: 'same-origin' })
        .then(function (r) { return r.json(); })
        .then(function (data) {
          list.textContent = '';
          if (!data.success) { list.appendChild(el('div', 'text-danger', data.error || 'Frete indisponível para este endereço.')); return; }
          if (data.simulated) list.appendChild(el('div', 'alert alert-warning py-1 small', 'Valores SIMULADOS (modo de desenvolvimento) - não são cotações reais.'));
          data.options.forEach(function (o, idx) {
            var label = el('label', 'list-group-item shipping-option d-flex justify-content-between align-items-center');
            var left = el('div');
            var radio = document.createElement('input');
            radio.type = 'radio'; radio.name = 'shipOption'; radio.className = 'form-check-input me-2';
            radio.addEventListener('change', function () { hidden.value = o.code; submit.disabled = co.getAttribute('data-payment-enabled') !== 'true'; setShipping(o.price); });
            left.appendChild(radio);
            left.appendChild(document.createTextNode(o.name + ' - ' + o.days + ' dia(s) úteis'));
            label.appendChild(left);
            label.appendChild(el('strong', '', o.priceText));
            list.appendChild(label);
          });
        })
        .catch(function () { list.textContent = 'Não foi possível consultar o frete agora.'; });
    }

    co.querySelectorAll('input[name="AddressId"]').forEach(function (r) {
      r.addEventListener('change', function () { loadShipping(r.value); });
      if (r.checked) loadShipping(r.value);
    });
  }
})();
