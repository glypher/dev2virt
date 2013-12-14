<!-- saved from url=(0014)about:internet -->
// ===================================================================
// Author: Mihai Matei <support@2virt.com>
// WWW: http://www.2virt.com/
//
// NOTICE: You may use this code for any purpose, commercial or
// private, without any further permission from the author. You may
// remove this notice from your final code if you wish, however it is
// appreciated by the author if at least my web site address is kept.
// ===================================================================
// HISTORY
// ------------------------------------------------------------------
// January 12, 2010: Added ClientCallback function to enable calling
//   COM Interop enabled .Net code

function ClientCallback(theForm) {
	var inputs = theForm.elements;
	var values = '';
	for (var i = 0; i < inputs.length; i++) {
		var oInput = inputs[i];
		// get only input text elements
		if (oInput.type != 'text') { continue; }
		values = values + oInput.value + '\n'; 
	}
	// finally call our OLE client
	window.external.BrowserCallback(values);
}
