// Small smoke test to detect major style regressions.
// It checks a few critical computed styles and exposes a visible result badge.
(function () {
 function addResultNode() {
 var el = document.createElement('div');
 el.id = 'style-smoke-result';
 el.textContent = 'Style Smoke: pending';
 document.body.appendChild(el);
 return el;
 }

 function pass(node, msg) {
 node.textContent = 'Style Smoke: PASS - ' + msg;
 node.classList.remove('fail');
 node.classList.add('pass');
 node.style.display = 'block';
 }

 function fail(node, msg) {
 node.textContent = 'Style Smoke: FAIL - ' + msg;
 node.classList.remove('pass');
 node.classList.add('fail');
 node.style.display = 'block';
 }

 function getComputedPX(el, prop) {
 var v = window.getComputedStyle(el).getPropertyValue(prop);
 return parseFloat(v) ||0;
 }

 function runChecks() {
 var result = document.getElementById('style-smoke-result') || addResultNode();

 try {
 // Check: amazon-design sets primary link color via variable --amazon-link-teal.
 var a = document.createElement('a');
 a.style.color = 'var(--amazon-link-teal)';
 a.className = 'text-amazon-link';
 a.textContent = 'x';
 a.href = 'javascript:void(0)';
 document.body.appendChild(a);
 var c = window.getComputedStyle(a).color || '';
 document.body.removeChild(a);
 if (!c || c === 'rgb(0,0,0)' || c === 'rgba(0,0,0,0)') {
 fail(result, 'link color missing or not applied');
 return;
 }

 // Check: product-image-wrapper should provide an aspect ratio box
 var box = document.createElement('div');
 box.className = 'product-image-wrapper';
 box.style.width = '200px';
 box.style.visibility = 'hidden';
 document.body.appendChild(box);
 var expectedHeight = box.clientWidth *0.75; //75% padding-top
 var actualHeight = box.clientHeight || box.offsetHeight;
 document.body.removeChild(box);
 if (Math.abs(actualHeight - expectedHeight) >6) {
 // allow small rounding differences
 fail(result, 'product image wrapper aspect ratio changed');
 return;
 }

 // Check: floating chat button has fixed position
 var btn = document.createElement('button');
 btn.className = 'floating-chat-btn';
 btn.style.visibility = 'hidden';
 document.body.appendChild(btn);
 var pos = window.getComputedStyle(btn).position;
 document.body.removeChild(btn);
 if (pos !== 'fixed') {
 fail(result, 'floating chat position changed');
 return;
 }

 pass(result, 'core checks OK');
 } catch (e) {
 fail(result, 'exception: ' + (e && e.message));
 }
 }

 if (document.readyState === 'complete' || document.readyState === 'interactive') {
 setTimeout(runChecks,120);
 } else {
 document.addEventListener('DOMContentLoaded', function () { setTimeout(runChecks,120); });
 }
})();
