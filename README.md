## SecureSubmit nopCommerce Payment Gateway

This extension allows nopCommerce to use the Heartland Payment Systems Gateway. All card data is tokenized using Heartland's SecureSubmit product.

## Installation

There are two directories in our GitHub repo:

Nop.Plugin.Payments.SecureSubmit - Contains the full plugin source

Payments.SecureSubmit - Contains only the binaries and files necessary to run the plugin

Use the directory based on the installation method you would like to use:

####Project Addition Method

Copy the 'Nop.Plugin.Payments.SecureSubmit' directory into the Nop Plugin directory. Load the NOP solution, right click on the solution and select ADD > EXISTING PROJECT..	selecting the secure submit project file. Build the soltuion.

####Minimal Files Method

Drag and drop the 'Payments.SecureSubmit' directory from the repo to '<nop directory root>\Presentation\Nop.Web\Plugins\'. 

## Contributing

1. Fork it
2. Create your feature branch (`git checkout -b my-new-feature`)
3. Commit your changes (`git commit -am 'Add some feature'`)
4. Push to the branch (`git push origin my-new-feature`)
5. Create new Pull Request
