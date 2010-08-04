version = File.read(File.expand_path("../VERSION",__FILE__)).strip

Gem::Specification.new do |spec|
  spec.platform    = Gem::Platform::RUBY
  spec.name        = 'fake'
  spec.version     = version
  spec.files       = Dir['lib/**/*'] + Dir['docs/**/*']
  
  spec.summary     = 'FAKE - F# Make - Get rid of the noise in your build scripts.'
  spec.description = 'FAKE - F# Make - is a build automation tool for .NET. Tasks and dependencies are specified in a DSL which is integrated in F#.'      
  
  spec.authors           = 'Steffen Forkmann'
  spec.email             = 'forkmann@gmx.de'
  spec.homepage          = 'http://github.com/forki/fake'
  spec.rubyforge_project = 'fake'
end