(function () {
    'use strict';

    var app = angular
        .module('app')
        .controller('albumsController', albumsController);
    
    albumsController.$inject = ['$scope', '$location', '$timeout', 'albumService'];
    

    function albumsController($scope,  $location, $timeout, albumService) {        
        var vm = this;       // this is the Model!

        vm.busy = false;
        vm.albums = null;   
        vm.error = {
            message: null,
            icon: "warning",
            reset: function () { vm.error = { message: "", icon: "warning" } }
        };


        // filled view event emit from root form
        vm.searchText = '';

        vm.artistpk = 0;

        vm.albumClick = function (album) {
            albumService.listScrollPos = $("#MainView").scrollTop();           
            $location.path( "/album/" + album.Id);
        };
        vm.getAlbums = function () {
            vm.busy = true;
            albumService.getAlbums() 
                .success(function (data) {                   
                    if (albumService.listScrollPos)
                        // delay is long to account for CSS animation
                        // can't scroll while animation is going
                        $timeout(function () {
                            $("#MainView").scrollTop(albumService.listScrollPos);
                            albumService.listScrollPos = 0; //reset
                        }, 900);
                    vm.busy = false;
                    vm.albums = data;
                })
                .error(function (err) {
                    vm.busy = false;
                    vm.error.message = "Failed to load albums. " + err.message;
                });            
        }
        vm.addAlbum = function () {            
            albumService.album = albumService.newAlbum();
            albumService.updateAlbum(albumService.album);
            $location.path("/album/edit/" + albumService.album.Id);
        };
        vm.deleteAlbum = function (album) {
            // on purpose! - force explicit prompt to minimize vandalization of demo
            if(!confirm("Are you sure you want to delete this album?"))
                return;

            albumService.deleteAlbum(album)
                .success(function(){
                    vm.albums = albumService.albums;
                })
                .error(onPageError);
        };
        vm.albumsFilter = function (alb) {
            var search = vm.searchText.toLowerCase();
            if (!alb || !alb.Title)
                return false;

            if ( alb.Title.toLowerCase().indexOf(search) > -1 ||
                alb.Artist.ArtistName.toLowerCase().indexOf(search) > -1)
                return true;

            return false;
        };

        // forwarded from Header controller
        $scope.$root.$on('onsearchkey', function (e,searchText) {
            vm.searchText = searchText;            
        });

        // controller initialization
        vm.getAlbums();       
    }
})();
