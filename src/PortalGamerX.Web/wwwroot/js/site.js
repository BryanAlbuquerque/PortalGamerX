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

  // Checkout: o botão de pagamento só é liberado com um endereço de cobrança selecionado e o pagamento online disponível.
  var co = document.querySelector('[data-checkout]');
  if (co) {
    var submit = co.querySelector('[data-checkout-submit]');
    var paymentOn = co.getAttribute('data-payment-enabled') === 'true';
    var radios = co.querySelectorAll('input[name="AddressId"]');
    function refresh() {
      var picked = false;
      radios.forEach(function (r) { if (r.checked) picked = true; });
      if (submit) submit.disabled = !(paymentOn && picked);
    }
    radios.forEach(function (r) { r.addEventListener('change', refresh); });
    refresh();
  }
})();
