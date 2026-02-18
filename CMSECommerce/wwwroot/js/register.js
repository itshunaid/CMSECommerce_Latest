$(document).ready(function () {
    const $form = $('#registrationForm');
    const $submitBtn = $('#submitBtn');
    const $userInput = $('#UserInput');
    const $userError = $('#UserError');

    const $phoneInput = $('#PhoneInput');
    const $phoneError = $('#PhoneError');
    const $backupMobileContainer = $('#BackupMobileContainer');

    const $emailInput = $('#EmailInput');
    const $emailError = $('#EmailError');
    const $backupEmailContainer = $('#BackupEmailContainer');

    // --- Terms & Conditions Elements ---
    const $termsModal = new bootstrap.Modal(document.getElementById('termsModal'));
    const $modalAgreeBtn = $('#ModalAgreeBtn');
    const $scrollBox = $('#TermsScrollBox');
    const $termsSection = $('#TermsSection');

    // THE FIX: Select the Hidden Input and the Visual checkbox
    const $termsHiddenInput = $('#TermsCheckbox'); // This maps to asp-for="HasAcceptedTerms"
    const $visualCheckbox = $('#VisualCheckbox');

    // --- Regex Patterns ---
    const itsRegex = /^\d{8}$/;
    const indianMobileRegex = /^(?:\+91|0)?[6-9]\d{9}$/;
    const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;

    // --- 1. HANDLE REDIRECT FROM LOGIN PAGE ---
    const urlParams = new URLSearchParams(window.location.search);
    const prefilledUser = urlParams.get('username');

    if (prefilledUser) {
        const decodedUser = decodeURIComponent(prefilledUser);
        $userInput.val(decodedUser);
        processUserIdentifier(decodedUser);
    }

    // --- 2. TERMS AND CONDITIONS LOGIC ---

    // Open modal via links
    $(document).on('click', '#OpenTerms, #OpenTermsLegal', function (e) {
        e.preventDefault();
        $termsModal.show();
    });

    // Enable agree button only on scroll to bottom
    $scrollBox.on('scroll', function () {
        const scrollBottom = $(this)[0].scrollHeight - $(this).scrollTop() - $(this).outerHeight();
        if (scrollBottom <= 20) {
            $modalAgreeBtn.prop('disabled', false);
        }
    });

    // Handle Modal Agree Click (The Alternate Approach)
    $modalAgreeBtn.on('click', function () {
        // 1. Set the ACTUAL value that goes to the server in the hidden input
        $termsHiddenInput.val("true");

        // 2. Update the visual UI so the user sees a checkmark
        $visualCheckbox.prop('checked', true);

        // 3. Clear all error messages and formatting immediately
        $('#TermsError').text("");
        $termsSection.removeClass("terms-accepted").css("border", "none");

        // Add a success class for visual feedback
        $termsSection.addClass("terms-accepted");

        $termsModal.hide();
    });

    // --- 3. IDENTIFIER & TOGGLE LOGIC ---

    function validateAndGetType(val) {
        if (itsRegex.test(val)) return "ITS";
        if (indianMobileRegex.test(val)) return "Mobile";
        if (emailRegex.test(val)) return "Email";
        return null;
    }

    function processUserIdentifier(val) {
        if (val === "") { toggleLoginMode(false); return; }

        const inputType = validateAndGetType(val);
        const isDigitOnly = /^\d+$/.test(val);

        if (!inputType) {
            let errorMsg = "Please enter a valid Email, 10-digit Mobile, or 8-digit ITS number.";
            if (isDigitOnly && val.length === 10 && !indianMobileRegex.test(val)) {
                errorMsg = "Invalid Indian mobile. Must start with 6, 7, 8, or 9.";
            } else if (val.includes('@') && !emailRegex.test(val)) {
                errorMsg = "Please enter a valid email format.";
            }
            $userError.text(errorMsg);
            $userInput.addClass("input-error-border");
            return;
        }

        if (inputType === "Mobile") {
            const cleanPhone = val.replace(/\D/g, '').slice(-10);
            $phoneInput.val(cleanPhone);
            $backupMobileContainer.hide();
            $backupEmailContainer.show();
        }
        else if (inputType === "Email") {
            $emailInput.val(val);
            $backupEmailContainer.hide();
            $backupMobileContainer.show();
        }
        else { // ITS
            $backupEmailContainer.show();
            $backupMobileContainer.show();
        }

        performAjaxValidation(val, 'Username', $userError, $userInput, inputType);
    }

    function performAjaxValidation(value, fieldType, $errorElement, $inputElement, detectedType = null) {
        $.ajax({
            url: validateUrl,
            type: 'GET',
            data: { value: value, type: fieldType, detectedType: detectedType },
            success: function (res) {
                if (!res.isAvailable) {
                    if (fieldType === 'Username') {
                        toggleLoginMode(true, res.message);
                    } else {
                        $errorElement.text(res.message);
                        $inputElement.addClass("input-error-border");
                    }
                } else {
                    if (fieldType === 'Username') toggleLoginMode(false);
                    $errorElement.text("");
                    $inputElement.removeClass("input-error-border");
                }
            }
        });
    }

    function toggleLoginMode(isLogin, message = "") {
        if (isLogin) {
            $('#formHeader').text("Sign in");
            $('#registrationFields, #confirmPasswordGroup, #legalNotice, #footerLink, #passwordHint, #TermsSection').hide();
            $userError.html(`<div class="alert alert-warning p-2 small text-dark"><i class="fas fa-exclamation-triangle me-1 text-warning"></i> ${message}<br/>Please sign in below.</div>`);
            $submitBtn.text("Sign in");
            $form.attr('action', loginUrl);

            // Reset terms state for login
            $termsHiddenInput.val("false");
            $visualCheckbox.prop('checked', false);
        } else {
            $('#formHeader').text("Create account");
            $('#registrationFields, #confirmPasswordGroup, #legalNotice, #footerLink, #passwordHint, #TermsSection').show();

            const val = $userInput.val().trim();
            const type = validateAndGetType(val);
            if (type === "Mobile") $backupMobileContainer.hide();
            if (type === "Email") $backupEmailContainer.hide();

            $userError.text("");
            $submitBtn.text("Continue");
            $form.attr('action', registerUrl);
        }
    }

    // --- 4. EVENT LISTENERS & RESTRICTIONS ---

    $userInput.on('blur', function () {
        processUserIdentifier($(this).val().trim());
    });

    $emailInput.on('blur', function () {
        const val = $(this).val().trim();
        if (val === "") return;
        if (!emailRegex.test(val)) {
            $emailError.text("Enter a valid email address.");
            $(this).addClass("input-error-border");
            return;
        }
        performAjaxValidation(val, 'Email', $emailError, $emailInput);
    });

    $phoneInput.on('blur', function () {
        const val = $(this).val().trim();
        if (val === "") return;
        if (!indianMobileRegex.test(val)) {
            $phoneError.text("Enter a valid 10-digit Indian mobile number.");
            $(this).addClass("input-error-border");
            return;
        }
        performAjaxValidation(val, 'PhoneNumber', $phoneError, $phoneInput);
    });

    $('#PhoneInput').on('input', function () {
        this.value = this.value.replace(/\D/g, '');
    });

    $('#ConfirmPassInput, #PassInput').on('blur', function () {
        if ($form.attr('action').toLowerCase().includes('login')) return;
        const p1 = $('#PassInput').val();
        const p2 = $('#ConfirmPassInput').val();
        if (p2 !== "" && p1 !== p2) {
            $('#ConfirmPassError').text("Passwords must match.");
            $('#ConfirmPassInput').addClass("input-error-border");
        } else {
            $('#ConfirmPassError').text("");
            $('#ConfirmPassInput').removeClass("input-error-border");
        }
    });

    $('.amazon-input-field').on('input', function () {
        $(this).removeClass("input-error-border");
        const id = $(this).attr('id');
        if (id === 'UserInput') $userError.text("");
        if (id === 'EmailInput') $emailError.text("");
        if (id === 'PhoneInput') $phoneError.text("");
    });

    // FINAL FORM SUBMISSION GUARD
    $form.on('submit', function (e) {
        const isRegisterMode = $form.attr('action').toLowerCase().includes('register');

        if (isRegisterMode) {
            // Check the hidden input value
            const hasAgreed = $termsHiddenInput.val() === "true";

            if (!hasAgreed) {
                e.preventDefault();
                $('#TermsError').text("You must agree to the terms and conditions to continue.");
                $termsSection.css("border", "1px solid #d00").css("padding", "5px");

                // Scroll to terms if they missed it
                $('html, body').animate({
                    scrollTop: $termsSection.offset().top - 100
                }, 100);

                return false;
            }
        }
    });
});