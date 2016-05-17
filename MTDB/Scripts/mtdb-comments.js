var MTDB = (function (MTDB, $) {
    if (typeof $ === 'undefined') {
        throw new Error('mtdb-comments.js requires jQuery')
    }

    var comments = MTDB.comments || {};

    function setupReply() {
        $('.reply-comment').on('click', function (event) {
            event.preventDefault();

            var target = $(event.currentTarget).parent(),
                commentForm = $('#comment-form'),
                cancelReply = $('#cancel-reply'),
                parentId = target.find('input').val();
                
            // Set parentId on form
            commentForm.find('#parentId').val(parentId);

            // Show 'cancel reply'
            cancelReply.removeClass('hidden');

            // Move form to reply target
            commentForm.appendTo(target);
        });

        $('#cancel-reply').on('click', function (event) {
            event.preventDefault();

            var commentsRoot = $('#comments'),
                commentForm = $('#comment-form'),
                cancelReply = $('#cancel-reply');

            // Set parentId on form
            commentForm.find('#parentId').val('');

            // Show 'cancel reply'
            cancelReply.addClass('hidden');

            // Move form to reply target
            commentForm.appendTo(commentsRoot);
        });
    }

    $(document).ready(function () {
        if (MTDB.comments.pageUrl) {
            $.ajax({
                type: 'GET',
                url: "/comments?pageUrl=" + encodeURIComponent(MTDB.comments.pageUrl),
                dataType: 'html',
                success: function (result) {
                    //Create a Div around the Partial View and fill the result
                    $('#comments').html(result);

                    // Hook up events
                    setupReply();
                }
            });
        }
    });

    MTDB.comments = comments;
    return MTDB;
}(MTDB || {}, jQuery));
