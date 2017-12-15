<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Player.aspx.cs" Inherits="VideoPlayer" %>

<!DOCTYPE html>

<html>
<head>
    <title>Player</title>
    <link href="//vjs.zencdn.net/6.4.0/video-js.css" rel="stylesheet">

    <!-- If you'd like to support IE8 -->
    <script src="//vjs.zencdn.net/ie8/1.1.2/videojs-ie8.min.js"></script>
    <script src="//vjs.zencdn.net/6.4.0/video.js"></script>
    <script src="//cdnjs.cloudflare.com/ajax/libs/videojs-contrib-hls/5.12.2/videojs-contrib-hls.js"></script>
</head>
<body>
    <form id="form1" runat="server">
        <video id="my-video" class="video-js" controls preload="auto" width="640" height="264"
            poster="MY_VIDEO_POSTER.jpg" data-setup="{}">
            <source src='<%="/_video_/out/" + this.Video + "/master.m3u8"  %>' type='application/x-mpegurl'>
            <%--<source src="MY_VIDEO.webm" type='video/webm'>--%>
            <p class="vjs-no-js">
                To view this video please enable JavaScript, and consider upgrading to a web browser that
            <a href="http://videojs.com/html5-video-support/" target="_blank">supports HTML5 video</a>
            </p>
        </video>
    </form>
</body>
</html>
