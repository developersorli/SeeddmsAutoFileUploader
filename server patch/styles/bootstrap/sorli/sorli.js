
$( document ).ready(function() {

 $( "#dialogsorli" ).dialog({
    autoOpen : false, modal : true, show : "none", hide : "none",   buttons : {
        "Refresh" : function() {
            location.reload();   
			$(this).dialog("close");			
        }
      }
  });
});


window.onload = function what()
{

$("a.opendocumentviasorli").click(function() { 
	window.open(document.getElementById("sorliId").value);
    $("#dialogsorli").dialog('open');
    return false;
});

}